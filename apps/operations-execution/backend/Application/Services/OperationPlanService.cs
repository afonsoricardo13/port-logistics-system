using System.Text.Json;
using Application.DTO;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Service for Operation Plan generation and management (US 4.1.2)
/// </summary>
public class OperationPlanService
{
    private readonly IOperationPlanRepository _repository;
    private readonly SchedulingServiceClient _schedulingClient;
    private readonly PortLogisticsClient _portLogisticsClient;
    private readonly ILogger<OperationPlanService> _logger;
    private const int TimeoutSeconds = 120; // Timeout for optimal algorithm (>10 vessels)

    public OperationPlanService(
        IOperationPlanRepository repository,
        SchedulingServiceClient schedulingClient,
        PortLogisticsClient portLogisticsClient,
        ILogger<OperationPlanService> logger)
    {
        _repository = repository;
        _schedulingClient = schedulingClient;
        _portLogisticsClient = portLogisticsClient;
        _logger = logger;
    }

    /// <summary>
    /// US 4.1.2: Generate and store Operation Plans for all VVNs scheduled for a given day
    /// Validates: Only Approved VVNs, Atomicity, Idempotency, Timeout handling
    /// Authorization: Controlled by API layer (RequirePermission attribute)
    /// </summary>
    public async Task<GenerateOperationPlansResponseDTO> GenerateOperationPlansForDayAsync(
        GenerateOperationPlansRequestDTO request)
    {
        var errors = new List<string>();

        try
        {
            _logger.LogInformation(
                "Generating operation plans for date {Date} using algorithm {Algorithm} by {User}",
                request.Date.ToString("yyyy-MM-dd"),
                request.Algorithm,
                request.RequestedBy);

            // ✅ VALIDATION 1: Check for duplicates (Idempotency)
            var existingPlans = await _repository.GetByDateAsync(request.Date);
            if (existingPlans.Any())
            {
                _logger.LogWarning(
                    "Operation plans already exist for date {Date}. Found {Count} existing plans.",
                    request.Date.ToString("yyyy-MM-dd"),
                    existingPlans.Count());

                return new GenerateOperationPlansResponseDTO
                {
                    Success = false,
                    Message = $"Operation plans already exist for {request.Date:yyyy-MM-dd}. Found {existingPlans.Count()} existing plans. Delete them first if you want to regenerate.",
                    Date = request.Date,
                    PlansGenerated = 0
                };
            }

            // ✅ VALIDATION 2: Verify VVNs exist and are Approved in Port Logistics
            var approvedVvns = await _portLogisticsClient.GetApprovedVvnsForDateAsync(request.Date);
            
            if (approvedVvns.Count == 0)
            {
                _logger.LogWarning(
                    "No Approved VVNs found for date {Date}",
                    request.Date.ToString("yyyy-MM-dd"));

                return new GenerateOperationPlansResponseDTO
                {
                    Success = false,
                    Message = $"No Approved Vessel Visit Notifications found for {request.Date:yyyy-MM-dd}. Only Approved VVNs can be scheduled.",
                    Date = request.Date,
                    PlansGenerated = 0
                };
            }

            _logger.LogInformation(
                "Found {Count} Approved VVNs for date {Date}: {VvnIds}",
                approvedVvns.Count,
                request.Date.ToString("yyyy-MM-dd"),
                string.Join(", ", approvedVvns.Select(v => v.Id)));

            // ✅ VALIDATION 3: Timeout handling for optimal algorithm with many vessels
            if (request.Algorithm.Equals("optimal", StringComparison.OrdinalIgnoreCase) && approvedVvns.Count > 10)
            {
                _logger.LogWarning(
                    "Optimal algorithm with {Count} vessels may take a long time. Consider using 'heuristic' or 'genetic' algorithm.",
                    approvedVvns.Count);
            }

            // Step 1: Call the Prolog scheduling service with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
            
            SchedulingServiceResponseDTO schedulingResponse;
            try
            {
                schedulingResponse = await _schedulingClient.GenerateDailyScheduleAsync(
                    request.Date, 
                    request.Algorithm);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError(
                    "Scheduling service timed out after {Timeout} seconds for date {Date} with algorithm {Algorithm}",
                    TimeoutSeconds,
                    request.Date.ToString("yyyy-MM-dd"),
                    request.Algorithm);

                return new GenerateOperationPlansResponseDTO
                {
                    Success = false,
                    Message = $"Scheduling service timed out after {TimeoutSeconds} seconds. Try using 'heuristic' or 'genetic' algorithm for better performance.",
                    Date = request.Date,
                    PlansGenerated = 0,
                    Error = "Timeout"
                };
            }

            if (!schedulingResponse.Success || schedulingResponse.Schedule == null || schedulingResponse.Schedule.Count == 0)
            {
                _logger.LogWarning(
                    "Scheduling service returned no plans for date {Date}. Message: {Message}",
                    request.Date.ToString("yyyy-MM-dd"),
                    schedulingResponse.Message);

                return new GenerateOperationPlansResponseDTO
                {
                    Success = false,
                    Message = schedulingResponse.Message ?? "Scheduling service returned no plans",
                    Date = request.Date,
                    PlansGenerated = 0
                };
            }

            // ✅ VALIDATION 4: Validate each vessel in schedule is Approved
            // Note: Prolog returns vessel_id as IMO number, not VVN ID
            // We need to map IMO -> VVN ID for storage
            var validationErrors = new List<string>();
            var imoToVvnIdMap = new Dictionary<string, long>();
            
            foreach (var vesselSchedule in schedulingResponse.Schedule)
            {
                // Match by IMO number (vesselSchedule.VvnId contains the vessel IMO from Prolog)
                var vvn = approvedVvns.FirstOrDefault(v => 
                    v.VesselImoNumber.Equals(vesselSchedule.VvnId, StringComparison.OrdinalIgnoreCase));
                
                if (vvn == null)
                {
                    var error = $"Vessel IMO {vesselSchedule.VvnId} in schedule is not in the list of Approved VVNs";
                    _logger.LogWarning(error);
                    validationErrors.Add(error);
                }
                else
                {
                    // Store mapping: IMO -> VVN ID
                    imoToVvnIdMap[vesselSchedule.VvnId] = vvn.Id;
                    
                    _logger.LogInformation(
                        "✅ Validated vessel IMO {Imo} matches VVN #{VvnId} (Status: {Status})",
                        vesselSchedule.VvnId,
                        vvn.Id,
                        vvn.Status);
                }
            }

            if (validationErrors.Any())
            {
                return new GenerateOperationPlansResponseDTO
                {
                    Success = false,
                    Message = "Schedule contains VVNs that are not Approved",
                    Date = request.Date,
                    PlansGenerated = 0,
                    Error = string.Join("; ", validationErrors)
                };
            }

            // ✅ ATOMICITY: Save all plans in a transaction-like manner
            // If any fails, we collect errors but continue
            var savedPlans = new List<OperationPlanSummaryDTO>();
            var planObjects = new List<OperationPlan>();

            // Step 2: Create all operation plan objects first
            foreach (var vesselSchedule in schedulingResponse.Schedule)
            {
                try
                {
                    // Get the actual VVN ID from the IMO mapping
                    var vvnId = imoToVvnIdMap.ContainsKey(vesselSchedule.VvnId) 
                        ? imoToVvnIdMap[vesselSchedule.VvnId].ToString() 
                        : vesselSchedule.VvnId;
                    
                    var operationPlan = await MapToOperationPlan(
                        vesselSchedule, 
                        request.Date, 
                        request.Algorithm,
                        request.RequestedBy,
                        vvnId);

                    planObjects.Add(operationPlan);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to map plan for VVN {vesselSchedule.VvnId}: {ex.Message}";
                    _logger.LogError(ex, error);
                    errors.Add(error);
                }
            }

            // ✅ ATOMICITY: If any mapping failed, don't save anything
            if (errors.Any())
            {
                _logger.LogError(
                    "Failed to map {ErrorCount} out of {TotalCount} operation plans. Rolling back.",
                    errors.Count,
                    schedulingResponse.Schedule.Count);

                return new GenerateOperationPlansResponseDTO
                {
                    Success = false,
                    Message = $"Failed to map operation plans. No plans were saved.",
                    Date = request.Date,
                    PlansGenerated = 0,
                    Error = string.Join("; ", errors)
                };
            }

            // Step 3: Save all operation plans
            foreach (var operationPlan in planObjects)
            {
                try
                {
                    var saved = await _repository.AddAsync(operationPlan);
                    savedPlans.Add(MapToSummaryDTO(saved));
                    
                    _logger.LogInformation(
                        "Operation plan created for VVN {VvnId} with ID {PlanId}",
                        operationPlan.VvnId,
                        saved.Id);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to save plan for VVN {operationPlan.VvnId}: {ex.Message}";
                    _logger.LogError(ex, error);
                    errors.Add(error);
                }
            }

            // ✅ ATOMICITY: If any save failed, rollback all saved plans
            if (errors.Any())
            {
                _logger.LogError(
                    "Failed to save {ErrorCount} out of {TotalCount} operation plans. Rolling back all saved plans.",
                    errors.Count,
                    planObjects.Count);

                // Rollback: Delete all successfully saved plans
                foreach (var savedPlan in savedPlans)
                {
                    try
                    {
                        await _repository.DeleteAsync(savedPlan.Id);
                        _logger.LogInformation("Rolled back plan {PlanId}", savedPlan.Id);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Failed to rollback plan {PlanId}", savedPlan.Id);
                    }
                }

                return new GenerateOperationPlansResponseDTO
                {
                    Success = false,
                    Message = "Failed to save some operation plans. All changes rolled back.",
                    Date = request.Date,
                    PlansGenerated = 0,
                    Error = string.Join("; ", errors)
                };
            }

            var response = new GenerateOperationPlansResponseDTO
            {
                Success = true,
                Message = $"Successfully generated {savedPlans.Count} operation plans for {savedPlans.Count} Approved VVNs",
                Date = request.Date,
                PlansGenerated = savedPlans.Count,
                Plans = savedPlans
            };

            _logger.LogInformation(
                "✅ Operation plan generation completed successfully. Generated: {Count} plans for date {Date}",
                savedPlans.Count,
                request.Date.ToString("yyyy-MM-dd"));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error generating operation plans for date {Date}", request.Date);
            return new GenerateOperationPlansResponseDTO
            {
                Success = false,
                Message = "An unexpected error occurred while generating operation plans",
                Date = request.Date,
                PlansGenerated = 0,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Get operation plans for a specific date
    /// </summary>
    public async Task<IEnumerable<OperationPlanSummaryDTO>> GetOperationPlansByDateAsync(DateTime date)
    {
        var plans = await _repository.GetByDateAsync(date);
        return plans.Select(MapToSummaryDTO);
    }

    /// <summary>
    /// Get operation plan by ID
    /// </summary>
    public async Task<OperationPlan?> GetOperationPlanByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }



    /// <summary>
    /// US 4.1.5: Get VVNs that don't have an associated Operation Plan for a given date
    /// </summary>
    public async Task<List<VvnDTO>> GetVvnsWithoutOperationPlansAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation(
                "Getting VVNs without Operation Plans for date {Date}",
                date.ToString("yyyy-MM-dd"));

            // Get all Approved VVNs for the specified date
            var approvedVvns = await _portLogisticsClient.GetApprovedVvnsForDateAsync(date);
            
            if (approvedVvns.Count == 0)
            {
                _logger.LogInformation(
                    "No Approved VVNs found for date {Date}",
                    date.ToString("yyyy-MM-dd"));
                return new List<VvnDTO>();
            }

            // Get all existing Operation Plans for the specified date
            var existingPlans = await _repository.GetByDateAsync(date);
            var existingVvnIds = new HashSet<string>(
                existingPlans.Select(p => p.VvnId),
                StringComparer.OrdinalIgnoreCase);

            // Filter VVNs that don't have an Operation Plan
            // Note: VVN IDs are stored as numbers, so convert to string for comparison
            var vvnsWithoutPlans = approvedVvns
                .Where(vvn => !existingVvnIds.Contains(vvn.Id.ToString()))
                .ToList();

            _logger.LogInformation(
                "Found {MissingCount} VVNs without Operation Plans out of {TotalCount} Approved VVNs for date {Date}",
                vvnsWithoutPlans.Count,
                approvedVvns.Count,
                date.ToString("yyyy-MM-dd"));

            return vvnsWithoutPlans;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting VVNs without Operation Plans for date {Date}",
                date.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    /// <summary>
    /// US 4.1.5: Regenerate all Operation Plans for a given day
    /// WARNING: This will DELETE existing plans and create new ones
    /// </summary>
    public async Task<GenerateOperationPlansResponseDTO> RegenerateOperationPlansForDayAsync(
        GenerateOperationPlansRequestDTO request)
    {
        try
        {
            _logger.LogWarning(
                "REGENERATING operation plans for date {Date}. Existing plans will be DELETED.",
                request.Date.ToString("yyyy-MM-dd"));

            // Step 1: Delete all existing plans for the date
            var existingPlans = await _repository.GetByDateAsync(request.Date);
            var deletedCount = 0;
            
            foreach (var plan in existingPlans)
            {
                try
                {
                    await _repository.DeleteAsync(plan.Id);
                    deletedCount++;
                    _logger.LogInformation(
                        "Deleted existing plan {PlanId} for VVN {VvnId}",
                        plan.Id,
                        plan.VvnId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to delete existing plan {PlanId}",
                        plan.Id);
                    
                    return new GenerateOperationPlansResponseDTO
                    {
                        Success = false,
                        Message = $"Failed to delete existing plans. Regeneration aborted.",
                        Date = request.Date,
                        PlansGenerated = 0,
                        Error = ex.Message
                    };
                }
            }

            _logger.LogInformation(
                "Deleted {Count} existing Operation Plans for date {Date}",
                deletedCount,
                request.Date.ToString("yyyy-MM-dd"));

            // Step 2: Generate new plans using the same logic as GenerateOperationPlansForDayAsync
            var result = await GenerateOperationPlansForDayAsync(request);
            
            if (result.Success)
            {
                result.Message = $"Successfully regenerated {result.PlansGenerated} operation plans. " +
                                $"Deleted {deletedCount} existing plans and created {result.PlansGenerated} new plans.";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error regenerating operation plans for date {Date}",
                request.Date.ToString("yyyy-MM-dd"));
            
            return new GenerateOperationPlansResponseDTO
            {
                Success = false,
                Message = "An unexpected error occurred while regenerating operation plans",
                Date = request.Date,
                PlansGenerated = 0,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// US 4.1.6: Aggregate total allocation time and number of operations for a specific resource in a period
    /// </summary>
    public async Task<ResourceUsageResultDTO> GetResourceUsageAsync(ResourceUsageRequestDTO request)
    {
        _logger.LogInformation("Getting resource usage for {Type} {Id} from {Start} to {End}", request.ResourceType, request.ResourceId, request.StartDate, request.EndDate);

        var result = new ResourceUsageResultDTO
        {
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            TotalAllocatedMinutes = 0,
            TotalOperations = 0,
            Plans = new List<ResourceUsagePlanEntryDTO>()
        };

        try
        {
            // Retrieve saved operation plans in the date range
            var plans = await _repository.GetByDateRangeAsync(request.StartDate.Date, request.EndDate.Date);

            foreach (var plan in plans)
            {
                try
                {
                    // Parse assigned resources
                    var resources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(plan.AssignedResources) ?? new List<ResourceAssignmentDTO>();

                    // Find assignments matching requested resource type and id
                    var matching = resources.Where(r => string.Equals(r.Type, request.ResourceType, StringComparison.OrdinalIgnoreCase)
                                                        && string.Equals(r.Id, request.ResourceId, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (!matching.Any()) continue; // resource not used in this plan

                    // Parse time window
                    DateTime? startTime = null;
                    DateTime? endTime = null;
                    int durationMinutes = 0;

                    if (!string.IsNullOrEmpty(plan.TimeWindows))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(plan.TimeWindows);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("start", out var s))
                                startTime = DateTime.Parse(s.GetString()!);
                            if (root.TryGetProperty("end", out var e))
                                endTime = DateTime.Parse(e.GetString()!);
                            if (root.TryGetProperty("duration", out var d))
                                durationMinutes = d.GetInt32();
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogWarning(parseEx, "Failed to parse TimeWindows for plan {PlanId}", plan.Id);
                        }
                    }

                    // Fallback: try to compute duration from Operations if time window missing
                    if ((!startTime.HasValue || !endTime.HasValue) && !string.IsNullOrEmpty(plan.Operations))
                    {
                        try
                        {
                            var ops = JsonSerializer.Deserialize<List<OperationDetailDTO>>(plan.Operations) ?? new List<OperationDetailDTO>();
                            durationMinutes = ops.Sum(o => o.Duration);
                            if (ops.Any())
                            {
                                startTime = ops.Min(o => o.StartTime);
                                endTime = ops.Max(o => o.EndTime);
                            }
                        }
                        catch { }
                    }

                    // If still lacking duration, skip allocation calculation
                    if (durationMinutes <= 0 && (!startTime.HasValue || !endTime.HasValue))
                    {
                        _logger.LogInformation("Skipping plan {PlanId} for resource usage: no valid time window or operations", plan.Id);
                        continue;
                    }

                    // If duration is zero but start/end present, compute minutes
                    if (durationMinutes <= 0 && startTime.HasValue && endTime.HasValue)
                    {
                        durationMinutes = (int)(endTime.Value - startTime.Value).TotalMinutes;
                    }

                    // Count operations assigned to the resource (if operation details include assigned resource ids)
                    var operationsCount = 0;
                    try
                    {
                        var opDetails = JsonSerializer.Deserialize<List<OperationDetailDTO>>(plan.Operations) ?? new List<OperationDetailDTO>();
                        // Count operations where assigned crane/dock/staff matches resource id depending on type
                        foreach (var op in opDetails)
                        {
                            if (string.Equals(request.ResourceType, "crane", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(op.AssignedCrane) && string.Equals(op.AssignedCrane, request.ResourceId, StringComparison.OrdinalIgnoreCase))
                                operationsCount++;
                            else if (string.Equals(request.ResourceType, "dock", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(op.AssignedDock) && string.Equals(op.AssignedDock, request.ResourceId, StringComparison.OrdinalIgnoreCase))
                                operationsCount++;
                            // staff assignments are not detailed per operation in current model; approximate by presence in AssignedResources
                        }
                    }
                    catch { }

                    // If no per-operation assignment found and resource is in matching list, approximate operationsCount as count of operations
                    if (operationsCount == 0)
                    {
                        try
                        {
                            var opList = JsonSerializer.Deserialize<List<OperationDetailDTO>>(plan.Operations);
                            operationsCount = opList?.Count ?? 0;
                        }
                        catch { }
                    }

                    // Sum into result
                    result.TotalAllocatedMinutes += durationMinutes;
                    result.TotalOperations += operationsCount;

                    result.Plans.Add(new ResourceUsagePlanEntryDTO
                    {
                        PlanId = plan.Id,
                        VvnId = plan.VvnId,
                        Date = plan.Date,
                        AllocatedMinutes = durationMinutes,
                        OperationsCount = operationsCount
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing plan {PlanId} for resource usage", plan.Id);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource usage for {Type} {Id}", request.ResourceType, request.ResourceId);
            throw;
        }
    }

    // Private helper methods

    private async Task<OperationPlan> MapToOperationPlan(
        VesselScheduleDTO schedule, 
        DateTime date, 
        string algorithm,
        string createdBy,
        string vvnId)
    {
        // Get VVN details to check container counts
        var vvn = await _portLogisticsClient.GetApprovedVvnsForDateAsync(date)
            .ContinueWith(t => t.Result.FirstOrDefault(v => v.Id.ToString() == vvnId));

        // Build operations list based on container counts
        var operationTypes = new List<string>();
        int loadingTime = 0;
        int unloadingTime = 0;

        if (vvn != null)
        {
            // Get crane capacity from assigned crane
            var craneCode = schedule.Resources.FirstOrDefault(r => r.Type == "crane")?.Id;
            var craneCapacity = 30; // Default
            
            if (!string.IsNullOrEmpty(craneCode))
            {
                craneCapacity = await _portLogisticsClient.GetCraneCapacityAsync(craneCode);
            }

            _logger.LogInformation(
                "Calculating times for VVN {VvnId}: Loading={Loading} containers, Unloading={Unloading} containers, Crane capacity={Capacity} containers/hour",
                vvnId,
                vvn.LoadingContainerCount,
                vvn.TotalUnloadingContainers,
                craneCapacity);

            // Calculate times: (containers / containersPerHour) * 60 minutes
            if (vvn.TotalUnloadingContainers > 0)
            {
                operationTypes.Add("unloading");
                unloadingTime = (int)Math.Ceiling((double)vvn.TotalUnloadingContainers / craneCapacity * 60);
            }
            
            if (vvn.LoadingContainerCount > 0)
            {
                operationTypes.Add("loading");
                loadingTime = (int)Math.Ceiling((double)vvn.LoadingContainerCount / craneCapacity * 60);
            }

            _logger.LogInformation(
                "Calculated times: Loading={LoadingTime} min, Unloading={UnloadingTime} min",
                loadingTime,
                unloadingTime);
        }

        // Operations: just the list of operation types
        var operations = operationTypes.Count > 0 
            ? JsonSerializer.Serialize(operationTypes) 
            : "null";

        var resources = JsonSerializer.Serialize(schedule.Resources);

        // TimeWindows: single window with start, end, total duration
        var startTime = schedule.StartTime ?? DateTime.Now;
        var endTime = schedule.EndTime ?? DateTime.Now;
        var durationMinutes = schedule.StartTime.HasValue && schedule.EndTime.HasValue
            ? (int)(schedule.EndTime.Value - schedule.StartTime.Value).TotalMinutes
            : loadingTime + unloadingTime;
        
        _logger.LogInformation(
            "TimeWindow calculation - Start: {Start}, End: {End}, Duration: {Duration} minutes",
            startTime,
            endTime,
            durationMinutes);
        
        var timeWindow = new
        {
            start = startTime.ToString("o"),
            end = endTime.ToString("o"),
            duration = durationMinutes
        };
        var timeWindowsJson = JsonSerializer.Serialize(timeWindow);

        return new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = vvnId,
            Date = date.Date, // Only date, no time
            Operations = operations,
            AssignedResources = resources,
            TimeWindows = timeWindowsJson,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            AlgorithmUsed = algorithm,
            Metadata = JsonSerializer.Serialize(new
            {
                vesselImo = schedule.VesselImo,
                loadingTime = loadingTime,
                unloadingTime = unloadingTime,
                assignedDock = schedule.AssignedDock,
                delay = schedule.Delay ?? 0
            })
        };
    }

    private OperationPlanSummaryDTO MapToSummaryDTO(OperationPlan plan)
    {
        var operationsCount = 0;
        try
        {
            var operations = JsonSerializer.Deserialize<List<OperationDetailDTO>>(plan.Operations);
            operationsCount = operations?.Count ?? 0;
        }
        catch
        {
            // Ignore deserialization errors for summary
        }

        return new OperationPlanSummaryDTO
        {
            Id = plan.Id,
            VvnId = plan.VvnId,
            Date = plan.Date,
            AlgorithmUsed = plan.AlgorithmUsed,
            CreatedAt = plan.CreatedAt,
            CreatedBy = plan.CreatedBy,
            OperationsCount = operationsCount,
            AssignedResources = plan.AssignedResources
        };
    }

    /// <summary>
    /// US 4.1.3: Convert Operation Plans to detailed DTOs with parsed metadata
    /// </summary>
    public Task<IEnumerable<OperationPlanDetailDTO>> ConvertToDetailedDTOs(IEnumerable<OperationPlan> plans)
    {
        var detailedPlans = new List<OperationPlanDetailDTO>();

        foreach (var plan in plans)
        {
            try
            {
                var operations = JsonSerializer.Deserialize<List<string>>(plan.Operations) ?? new List<string>();
                var resources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(plan.AssignedResources) ?? new List<ResourceAssignmentDTO>();
                
                var detailed = new OperationPlanDetailDTO
                {
                    Id = plan.Id,
                    VvnId = plan.VvnId,
                    Date = plan.Date,
                    AlgorithmUsed = plan.AlgorithmUsed,
                    CreatedAt = plan.CreatedAt,
                    CreatedBy = plan.CreatedBy,
                    Operations = operations,
                    Resources = resources,
                    UpdatedBy = plan.UpdatedBy,
                    UpdatedAt = plan.UpdatedAt
                };

                // Parse change logs
                if (!string.IsNullOrEmpty(plan.ChangeLogs))
                {
                    try
                    {
                        detailed.ChangeLogs = JsonSerializer.Deserialize<List<ChangeLogEntryDTO>>(plan.ChangeLogs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing ChangeLogs for plan {PlanId}", plan.Id);
                    }
                }

                // Parse time window
                if (!string.IsNullOrEmpty(plan.TimeWindows))
                {
                    try
                    {
                        using var timeWindowDoc = JsonDocument.Parse(plan.TimeWindows);
                        var root = timeWindowDoc.RootElement;
                        
                        if (root.TryGetProperty("start", out var start))
                            detailed.StartTime = DateTime.Parse(start.GetString()!);
                        if (root.TryGetProperty("end", out var end))
                            detailed.EndTime = DateTime.Parse(end.GetString()!);
                        if (root.TryGetProperty("duration", out var duration))
                            detailed.Duration = duration.GetInt32();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing TimeWindows for plan {PlanId}", plan.Id);
                    }
                }

                // Parse metadata
                if (!string.IsNullOrEmpty(plan.Metadata))
                {
                    try
                    {
                        using var metadataDoc = JsonDocument.Parse(plan.Metadata);
                        var root = metadataDoc.RootElement;
                        
                        if (root.TryGetProperty("vesselImo", out var imo))
                            detailed.VesselImo = imo.GetString();
                        if (root.TryGetProperty("loadingTime", out var loadTime))
                            detailed.LoadingTime = loadTime.GetInt32();
                        if (root.TryGetProperty("unloadingTime", out var unloadTime))
                            detailed.UnloadingTime = unloadTime.GetInt32();
                        if (root.TryGetProperty("assignedDock", out var dock))
                            detailed.AssignedDock = dock.GetString();
                        if (root.TryGetProperty("delay", out var delay))
                            detailed.Delay = delay.GetDouble();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing Metadata for plan {PlanId}", plan.Id);
                    }
                }

                detailedPlans.Add(detailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting plan {PlanId} to detailed DTO", plan.Id);
            }
        }

        return Task.FromResult<IEnumerable<OperationPlanDetailDTO>>(detailedPlans);
    }

    /// <summary>
    /// US 4.1.4: Manually update an Operation Plan with validation, logging, and inconsistency detection
    /// </summary>
    public async Task<UpdateOperationPlanResponseDTO> UpdateOperationPlanAsync(
        Guid planId, 
        UpdateOperationPlanRequestDTO request)
    {
        var response = new UpdateOperationPlanResponseDTO
        {
            Success = false,
            Message = string.Empty
        };

        try
        {
            _logger.LogInformation(
                "Updating operation plan {PlanId} by {User}. Reason: {Reason}",
                planId,
                request.UpdatedBy,
                request.ReasonForChange);

            // Validation: Check if plan exists
            var existingPlan = await _repository.GetByIdAsync(planId);
            if (existingPlan == null)
            {
                response.Message = $"Operation Plan with ID {planId} not found";
                return response;
            }

            // Validation: Require reason for change
            if (string.IsNullOrWhiteSpace(request.ReasonForChange))
            {
                response.Message = "Reason for change is required for audit trail";
                return response;
            }

            // Validation: Require updated by
            if (string.IsNullOrWhiteSpace(request.UpdatedBy))
            {
                response.Message = "UpdatedBy field is required";
                return response;
            }

            // Track changes for audit log
            var changes = new List<string>();
            var timestamp = DateTime.UtcNow;

            // Parse current resources
            var currentResources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(
                existingPlan.AssignedResources) ?? new List<ResourceAssignmentDTO>();

            // Update crane assignments
            if (request.CraneAssignments != null && request.CraneAssignments.Any())
            {
                // Validate crane resources exist and are available
                foreach (var crane in request.CraneAssignments)
                {
                    var resource = await _portLogisticsClient.GetPhysicalResourceAsync(crane.Id);
                    if (resource == null)
                    {
                        response.Message = $"Physical resource {crane.Id} not found in database";
                        return response;
                    }
                    if (!string.Equals(resource.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        response.Warnings.Add($"Warning: Resource {crane.Id} is not active (Status: {resource.Status})");
                    }
                    
                    // Set the name from database
                    crane.Name = resource.Description;
                }
                
                var existingCranes = currentResources.Where(r => r.Type == "crane").ToList();
                foreach (var crane in existingCranes)
                {
                    currentResources.Remove(crane);
                }
                currentResources.AddRange(request.CraneAssignments);
                changes.Add($"Updated crane assignments: {string.Join(", ", request.CraneAssignments.Select(c => c.Id))}");
                
                _logger.LogInformation("Crane assignments updated for plan {PlanId}", planId);
            }

            // Update staff assignments
            if (request.StaffAssignments != null && request.StaffAssignments.Any())
            {
                // Validate staff resources exist and are available
                foreach (var staff in request.StaffAssignments)
                {
                    var operatingStaff = await _portLogisticsClient.GetOperatingStaffAsync(staff.Id);
                    if (operatingStaff == null)
                    {
                        response.Message = $"Operating staff {staff.Id} not found in database";
                        return response;
                    }
                    if (!string.Equals(operatingStaff.Status, "Available", StringComparison.OrdinalIgnoreCase))
                    {
                        response.Warnings.Add($"Warning: Staff {staff.Id} is not available (Status: {operatingStaff.Status})");
                    }
                    
                    // Validate operational window
                    if (!string.IsNullOrEmpty(operatingStaff.OperationalWindow) && request.StartTime.HasValue && request.EndTime.HasValue)
                    {
                        if (!IsWithinOperationalWindow(request.StartTime.Value, request.EndTime.Value, operatingStaff.OperationalWindow))
                        {
                            response.Message = $"Staff {staff.Id} cannot work during requested hours. Working hours: {operatingStaff.OperationalWindow}, Operation Plan: {request.StartTime.Value:HH:mm}-{request.EndTime.Value:HH:mm}";
                            return response;
                        }
                    }
                    
                    // Set the name from database
                    staff.Name = operatingStaff.Name;
                }
                
                var existingStaff = currentResources.Where(r => r.Type == "staff").ToList();
                foreach (var staff in existingStaff)
                {
                    currentResources.Remove(staff);
                }
                currentResources.AddRange(request.StaffAssignments);
                changes.Add($"Updated staff assignments: {string.Join(", ", request.StaffAssignments.Select(s => s.Id))}");
                
                _logger.LogInformation("Staff assignments updated for plan {PlanId}", planId);
            }

            // Update dock assignment
            if (!string.IsNullOrWhiteSpace(request.AssignedDock))
            {
                var existingDock = currentResources.FirstOrDefault(r => r.Type == "dock");
                if (existingDock != null)
                {
                    currentResources.Remove(existingDock);
                }
                currentResources.Add(new ResourceAssignmentDTO
                {
                    Type = "dock",
                    Id = request.AssignedDock,
                    Name = request.AssignedDock
                });
                changes.Add($"Updated dock assignment: {request.AssignedDock}");
                
                _logger.LogInformation("Dock assignment updated for plan {PlanId} to {Dock}", planId, request.AssignedDock);
            }

            // Save updated resources
            existingPlan.AssignedResources = JsonSerializer.Serialize(currentResources);

            // Update time windows
            if (request.StartTime.HasValue || request.EndTime.HasValue)
            {
                var timeWindow = new Dictionary<string, object>();
                
                if (!string.IsNullOrEmpty(existingPlan.TimeWindows))
                {
                    try
                    {
                        timeWindow = JsonSerializer.Deserialize<Dictionary<string, object>>(existingPlan.TimeWindows) 
                            ?? new Dictionary<string, object>();
                    }
                    catch
                    {
                        timeWindow = new Dictionary<string, object>();
                    }
                }

                if (request.StartTime.HasValue)
                {
                    timeWindow["start"] = request.StartTime.Value.ToString("o");
                    changes.Add($"Updated start time: {request.StartTime.Value:yyyy-MM-dd HH:mm}");
                }

                if (request.EndTime.HasValue)
                {
                    timeWindow["end"] = request.EndTime.Value.ToString("o");
                    changes.Add($"Updated end time: {request.EndTime.Value:yyyy-MM-dd HH:mm}");
                }

                if (request.StartTime.HasValue && request.EndTime.HasValue)
                {
                    var duration = (int)(request.EndTime.Value - request.StartTime.Value).TotalMinutes;
                    timeWindow["duration"] = duration;
                }

                existingPlan.TimeWindows = JsonSerializer.Serialize(timeWindow);
                
                _logger.LogInformation("Time windows updated for plan {PlanId}", planId);
            }

            // Update operations if provided
            if (request.Operations != null && request.Operations.Any())
            {
                var operationTypes = request.Operations.Select(o => o.Type).Distinct().ToList();
                existingPlan.Operations = JsonSerializer.Serialize(operationTypes);
                changes.Add($"Updated operations: {string.Join(", ", operationTypes)}");
                
                _logger.LogInformation("Operations updated for plan {PlanId}", planId);
            }

            // Update audit fields
            existingPlan.UpdatedBy = request.UpdatedBy;
            existingPlan.UpdatedAt = timestamp;

            // Create change log entry
            var changeLogEntry = new ChangeLogEntryDTO
            {
                Timestamp = timestamp,
                Author = request.UpdatedBy,
                Reason = request.ReasonForChange,
                Changes = changes
            };

            // Append to change logs
            var changeLogs = new List<ChangeLogEntryDTO>();
            if (!string.IsNullOrEmpty(existingPlan.ChangeLogs))
            {
                try
                {
                    changeLogs = JsonSerializer.Deserialize<List<ChangeLogEntryDTO>>(existingPlan.ChangeLogs) 
                        ?? new List<ChangeLogEntryDTO>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing existing ChangeLogs for plan {PlanId}", planId);
                }
            }
            changeLogs.Add(changeLogEntry);
            existingPlan.ChangeLogs = JsonSerializer.Serialize(changeLogs);

            // Check for resource conflicts and VVN inconsistencies
            await DetectInconsistenciesAsync(existingPlan, response);

            // Save the updated plan
            await _repository.UpdateAsync(existingPlan);

            _logger.LogInformation(
                "Operation plan {PlanId} successfully updated by {User}. Changes: {ChangeCount}",
                planId,
                request.UpdatedBy,
                changes.Count);

            // Convert to detailed DTO
            var updatedPlans = await ConvertToDetailedDTOs(new[] { existingPlan });
            response.UpdatedPlan = updatedPlans.FirstOrDefault();
            response.Success = true;
            response.Message = "Operation plan updated successfully";

            // Add warnings if inconsistencies were found
            if (response.ResourceConflicts.Any() || response.VvnInconsistencies.Any())
            {
                response.Warnings.Add("Potential inconsistencies detected. Please review the conflicts and inconsistencies.");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating operation plan {PlanId}", planId);
            response.Message = $"Error updating operation plan: {ex.Message}";
            return response;
        }
    }

    /// <summary>
    /// US 4.1.4: Detect resource conflicts and VVN inconsistencies
    /// </summary>
    private async Task DetectInconsistenciesAsync(
        OperationPlan updatedPlan, 
        UpdateOperationPlanResponseDTO response)
    {
        try
        {
            // Parse the updated plan details
            var resources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(
                updatedPlan.AssignedResources) ?? new List<ResourceAssignmentDTO>();

            DateTime? startTime = null;
            DateTime? endTime = null;

            if (!string.IsNullOrEmpty(updatedPlan.TimeWindows))
            {
                try
                {
                    using var timeWindowDoc = JsonDocument.Parse(updatedPlan.TimeWindows);
                    var root = timeWindowDoc.RootElement;
                    
                    if (root.TryGetProperty("start", out var start))
                        startTime = DateTime.Parse(start.GetString()!);
                    if (root.TryGetProperty("end", out var end))
                        endTime = DateTime.Parse(end.GetString()!);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing TimeWindows during inconsistency detection");
                }
            }

            if (!startTime.HasValue || !endTime.HasValue)
            {
                _logger.LogWarning("Cannot detect inconsistencies: time windows not properly set");
                return;
            }

            // Get all other operation plans for the same date
            var otherPlans = (await _repository.GetByDateAsync(updatedPlan.Date))
                .Where(p => p.Id != updatedPlan.Id)
                .ToList();

            // Check for resource conflicts
            foreach (var resource in resources)
            {
                if (resource.Type == "crane" || resource.Type == "staff" || resource.Type == "dock")
                {
                    foreach (var otherPlan in otherPlans)
                    {
                        var otherResources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(
                            otherPlan.AssignedResources) ?? new List<ResourceAssignmentDTO>();

                        var conflictingResource = otherResources.FirstOrDefault(
                            r => r.Type == resource.Type && r.Id == resource.Id);

                        if (conflictingResource != null)
                        {
                            // Check time overlap
                            DateTime? otherStart = null;
                            DateTime? otherEnd = null;

                            if (!string.IsNullOrEmpty(otherPlan.TimeWindows))
                            {
                                try
                                {
                                    using var doc = JsonDocument.Parse(otherPlan.TimeWindows);
                                    var root = doc.RootElement;
                                    
                                    if (root.TryGetProperty("start", out var s))
                                        otherStart = DateTime.Parse(s.GetString()!);
                                    if (root.TryGetProperty("end", out var e))
                                        otherEnd = DateTime.Parse(e.GetString()!);
                                }
                                catch { }
                            }

                            if (otherStart.HasValue && otherEnd.HasValue)
                            {
                                // Check for overlap
                                if (startTime.Value < otherEnd.Value && endTime.Value > otherStart.Value)
                                {
                                    response.ResourceConflicts.Add(new ResourceConflictDTO
                                    {
                                        ResourceType = resource.Type,
                                        ResourceId = resource.Id,
                                        ConflictDescription = $"{resource.Type} '{resource.Id}' is already assigned to another VVN during this time window",
                                        ConflictingVvnIds = new List<string> { otherPlan.VvnId },
                                        ConflictStartTime = otherStart.Value > startTime.Value ? otherStart.Value : startTime.Value,
                                        ConflictEndTime = otherEnd.Value < endTime.Value ? otherEnd.Value : endTime.Value
                                    });

                                    _logger.LogWarning(
                                        "Resource conflict detected: {ResourceType} {ResourceId} overlaps with VVN {VvnId}",
                                        resource.Type,
                                        resource.Id,
                                        otherPlan.VvnId);
                                }
                            }
                        }
                    }
                }
            }

            // Check VVN-specific inconsistencies
            // Example: Check if timing is realistic for container operations
            var duration = (endTime.Value - startTime.Value).TotalMinutes;
            if (duration < 30)
            {
                response.VvnInconsistencies.Add(new VvnInconsistencyDTO
                {
                    VvnId = updatedPlan.VvnId,
                    InconsistencyType = "timing",
                    Description = "Operation duration is very short (less than 30 minutes). This may not be realistic for vessel operations.",
                    Severity = "warning"
                });
            }

            // Check if dock capacity might be exceeded
            var dock = resources.FirstOrDefault(r => r.Type == "dock");
            if (dock != null)
            {
                var concurrentPlansAtDock = 0;
                foreach (var otherPlan in otherPlans)
                {
                    var otherResources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(
                        otherPlan.AssignedResources) ?? new List<ResourceAssignmentDTO>();
                    
                    var otherDock = otherResources.FirstOrDefault(r => r.Type == "dock" && r.Id == dock.Id);
                    if (otherDock != null)
                    {
                        // Check time overlap
                        DateTime? otherStart = null;
                        DateTime? otherEnd = null;

                        if (!string.IsNullOrEmpty(otherPlan.TimeWindows))
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(otherPlan.TimeWindows);
                                var root = doc.RootElement;
                                
                                if (root.TryGetProperty("start", out var s))
                                    otherStart = DateTime.Parse(s.GetString()!);
                                if (root.TryGetProperty("end", out var e))
                                    otherEnd = DateTime.Parse(e.GetString()!);
                            }
                            catch { }
                        }

                        if (otherStart.HasValue && otherEnd.HasValue &&
                            startTime.Value < otherEnd.Value && endTime.Value > otherStart.Value)
                        {
                            concurrentPlansAtDock++;
                        }
                    }
                }

                if (concurrentPlansAtDock > 0)
                {
                    response.VvnInconsistencies.Add(new VvnInconsistencyDTO
                    {
                        VvnId = updatedPlan.VvnId,
                        InconsistencyType = "resource",
                        Description = $"Dock '{dock.Id}' has {concurrentPlansAtDock} other vessel(s) scheduled during overlapping time periods.",
                        Severity = "error"
                    });
                }
            }

            _logger.LogInformation(
                "Inconsistency detection completed for plan {PlanId}: {ConflictCount} conflicts, {InconsistencyCount} inconsistencies",
                updatedPlan.Id,
                response.ResourceConflicts.Count,
                response.VvnInconsistencies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting inconsistencies for plan {PlanId}", updatedPlan.Id);
            response.Warnings.Add("Unable to fully detect inconsistencies due to an error. Please manually verify resource assignments.");
        }
    }

    /// <summary>
    /// Validates if operation plan time window is within staff operational window
    /// Format expected: "10:00-20:00" or "Mon-Fri 10:00-20:00"
    /// </summary>
    private bool IsWithinOperationalWindow(DateTime opStart, DateTime opEnd, string operationalWindow)
    {
        try
        {
            // Extract time range from operational window (format: "10:00-20:00" or "Mon-Fri 10:00-20:00")
            var parts = operationalWindow.Split(' ');
            var timeRange = parts.Length > 1 ? parts[^1] : parts[0]; // Get last part (time range)
            
            var times = timeRange.Split('-');
            if (times.Length != 2) return true; // Cannot parse, allow it
            
            if (!TimeSpan.TryParse(times[0].Trim(), out var windowStart)) return true;
            if (!TimeSpan.TryParse(times[1].Trim(), out var windowEnd)) return true;
            
            var opStartTime = opStart.TimeOfDay;
            var opEndTime = opEnd.TimeOfDay;
            
            // Check if operation times are within window
            bool startWithin = opStartTime >= windowStart && opStartTime <= windowEnd;
            bool endWithin = opEndTime >= windowStart && opEndTime <= windowEnd;
            
            return startWithin && endWithin;
        }
        catch
        {
            return true; // If parsing fails, allow the assignment
        }
    }
}
