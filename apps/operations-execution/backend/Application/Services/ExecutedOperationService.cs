using Application.DTO;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services;

/// <summary>
/// US 4.1.9: Service for managing executed operations within vessel visit executions
/// </summary>
public class ExecutedOperationService
{
    private readonly IExecutedOperationRepository _repository;
    private readonly IVesselVisitExecutionRepository _vveRepository;
    private readonly IOperationPlanRepository _operationPlanRepository;
    private readonly ILogger<ExecutedOperationService> _logger;

    public ExecutedOperationService(
        IExecutedOperationRepository repository,
        IVesselVisitExecutionRepository vveRepository,
        IOperationPlanRepository operationPlanRepository,
        ILogger<ExecutedOperationService> logger)
    {
        _repository = repository;
        _vveRepository = vveRepository;
        _operationPlanRepository = operationPlanRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all executed operations for a specific VVE
    /// </summary>
    public async Task<IEnumerable<ExecutedOperationDTO>> GetOperationsByVveIdAsync(string vveId)
    {
        var operations = await _repository.GetByVveIdAsync(vveId);
        return operations.Select(MapToDTO);
    }

    /// <summary>
    /// Get executed operation by ID
    /// </summary>
    public async Task<ExecutedOperationDTO?> GetByIdAsync(Guid id)
    {
        var operation = await _repository.GetByIdAsync(id);
        return operation == null ? null : MapToDTO(operation);
    }

    /// <summary>
    /// US 4.1.9: Initialize executed operations from an operation plan
    /// This creates ExecutedOperation records from the planned operations
    /// </summary>
    public async Task<InitializeOperationsFromPlanResponseDTO> InitializeOperationsFromPlanAsync(
        string vveId, 
        Guid operationPlanId, 
        string operatorEmail)
    {
        var response = new InitializeOperationsFromPlanResponseDTO();

        // Validate VVE exists
        var vve = await _vveRepository.GetByIdAsync(vveId);
        if (vve == null)
        {
            response.Success = false;
            response.Message = $"VVE {vveId} not found";
            return response;
        }

        // Validate operation plan exists
        var plan = await _operationPlanRepository.GetByIdAsync(operationPlanId);
        if (plan == null)
        {
            response.Success = false;
            response.Message = $"Operation Plan {operationPlanId} not found";
            return response;
        }

        // Check if operations already initialized for this plan
        var existing = await _repository.GetByOperationPlanIdAsync(operationPlanId);
        if (existing.Any())
        {
            response.Success = false;
            response.Message = "Operations already initialized for this operation plan";
            response.Operations = existing.Select(MapToDTO).ToList();
            return response;
        }

        // Parse planned operations from the plan
        _logger.LogInformation("Operations JSON from plan {PlanId}: {Json}", operationPlanId, plan.Operations);
        _logger.LogInformation("TimeWindows JSON from plan {PlanId}: {Json}", operationPlanId, plan.TimeWindows);
        
        List<OperationDetailDTO>? plannedOperations = null;
        List<(DateTime start, DateTime end)>? timeWindows = null;
        
        // Parse TimeWindows first to get correct times
        try
        {
            using var timeDoc = JsonDocument.Parse(plan.TimeWindows);
            
            // Handle array of time windows (one per operation)
            if (timeDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                timeWindows = new List<(DateTime, DateTime)>();
                for (int i = 0; i < timeDoc.RootElement.GetArrayLength(); i++)
                {
                    var tw = timeDoc.RootElement[i];
                    var start = TryGetDateTime(tw, "start", "Start", "startTime", "StartTime") ?? DateTime.UtcNow.AddHours(i);
                    var end = TryGetDateTime(tw, "end", "End", "endTime", "EndTime") ?? start.AddHours(1);
                    timeWindows.Add((start, end));
                }
            }
            // Handle single time window object (Prolog format: {start, end, duration})
            else if (timeDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var start = TryGetDateTime(timeDoc.RootElement, "start", "Start", "startTime", "StartTime");
                var end = TryGetDateTime(timeDoc.RootElement, "end", "End", "endTime", "EndTime");
                var duration = TryGetInt(timeDoc.RootElement, "duration", "Duration");
                
                if (start.HasValue && end.HasValue && duration.HasValue)
                {
                    // Total time window - divide equally by number of operations
                    var totalMinutes = (end.Value - start.Value).TotalMinutes;
                    _logger.LogInformation("TimeWindows: Single window from {Start} to {End}, total {Total}min for {Count} operations", 
                        start, end, totalMinutes, plan.Operations);
                    
                    // We'll parse operations first to know how many there are, then divide time
                    // For now, store the full window and we'll split it per operation below
                    timeWindows = new List<(DateTime, DateTime)> { (start.Value, end.Value) };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse TimeWindows, will use default times");
        }
        
        // Try multiple parsing strategies for operations
        try
        {
            using var doc = JsonDocument.Parse(plan.Operations);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var count = doc.RootElement.GetArrayLength();
                plannedOperations = new List<OperationDetailDTO>();
                
                _logger.LogInformation("Found {Count} operations in plan", count);
                
                // If we have a single time window, divide it equally among operations
                if (timeWindows != null && timeWindows.Count == 1 && count > 1)
                {
                    var totalWindow = timeWindows[0];
                    var totalMinutes = (totalWindow.end - totalWindow.start).TotalMinutes;
                    var minutesPerOp = totalMinutes / count;
                    
                    timeWindows = new List<(DateTime, DateTime)>();
                    for (int i = 0; i < count; i++)
                    {
                        var opStart = totalWindow.start.AddMinutes(i * minutesPerOp);
                        var opEnd = totalWindow.start.AddMinutes((i + 1) * minutesPerOp);
                        timeWindows.Add((opStart, opEnd));
                    }
                    _logger.LogInformation("Split single time window into {Count} slots of {Minutes}min each", count, minutesPerOp);
                }
                
                for (int i = 0; i < count; i++)
                {
                    var element = doc.RootElement[i];
                    
                    // Get time window for this operation
                    DateTime startTime = timeWindows != null && i < timeWindows.Count ? timeWindows[i].start : DateTime.UtcNow.AddHours(i);
                    DateTime endTime = timeWindows != null && i < timeWindows.Count ? timeWindows[i].end : startTime.AddHours(1);
                    
                    // If it's a string, treat as operation type
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var opString = element.GetString() ?? "loading";
                        var opLower = opString.ToLower();
                        
                        // Check "unloading" first since "unloading" contains "loading"
                        string opType = opLower.Contains("unload") ? "unloading" : "loading";
                        
                        plannedOperations.Add(new OperationDetailDTO
                        {
                            Type = opType,
                            StartTime = startTime,
                            EndTime = endTime,
                            Duration = (int)(endTime - startTime).TotalMinutes
                        });
                    }
                    // If it's an object, extract properties
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        var opDto = new OperationDetailDTO
                        {
                            Type = element.TryGetProperty("type", out var typeProp) ? 
                                   typeProp.GetString() ?? "loading" : 
                                   element.TryGetProperty("Type", out var TypeProp) ?
                                   TypeProp.GetString() ?? "loading" : "loading",
                            StartTime = TryGetDateTime(element, "startTime", "StartTime") ?? startTime,
                            EndTime = TryGetDateTime(element, "endTime", "EndTime") ?? endTime,
                            Duration = TryGetInt(element, "duration", "Duration") ?? (int)(endTime - startTime).TotalMinutes
                        };
                        plannedOperations.Add(opDto);
                    }
                }
            }
            else
            {
                response.Success = false;
                response.Message = "Operations field is not a JSON array";
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse operations from plan {PlanId}", operationPlanId);
            response.Success = false;
            response.Message = $"Failed to parse operation plan data: {ex.Message}";
            return response;
        }
        
        if (plannedOperations == null || !plannedOperations.Any())
        {
            response.Success = false;
            response.Message = "No operations found in operation plan";
            return response;
        }

        // Create ExecutedOperation records for each planned operation
        var executedOperations = new List<ExecutedOperation>();
        for (int i = 0; i < plannedOperations.Count; i++)
        {
            var plannedOp = plannedOperations[i];
            var executedOp = new ExecutedOperation
            {
                Id = Guid.NewGuid(),
                VveId = vveId,
                OperationPlanId = operationPlanId,
                PlannedOperationIndex = i,
                Type = plannedOp.Type,
                Status = ExecutionStatus.Started, // Default to Started
                PlannedStartTime = plannedOp.StartTime,
                PlannedEndTime = plannedOp.EndTime,
                ActualStartTime = null, // To be filled by operator
                ActualEndTime = null,
                ActualResources = null,
                DelayMinutes = null,
                UpdatedBy = operatorEmail,
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Notes = null
            };

            var created = await _repository.AddAsync(executedOp);
            executedOperations.Add(created);
        }

        response.Success = true;
        response.Message = $"Successfully initialized {executedOperations.Count} operations from plan";
        response.OperationsInitialized = executedOperations.Count;
        response.Operations = executedOperations.Select(MapToDTO).ToList();

        _logger.LogInformation(
            "✅ US 4.1.9: Initialized {Count} operations for VVE {VveId} from plan {PlanId}",
            executedOperations.Count, vveId, operationPlanId);

        // US 4.1.9: Sync ExecutedOperations field in VVE
        await SyncVveExecutedOperationsAsync(vveId);

        return response;
    }

    /// <summary>
    /// US 4.1.9: Create a new executed operation manually (not from plan)
    /// </summary>
    public async Task<ExecutedOperationDTO> CreateAsync(CreateExecutedOperationDTO dto, string operatorEmail)
    {
        // Validate VVE exists
        var vve = await _vveRepository.GetByIdAsync(dto.VveId);
        if (vve == null)
        {
            throw new ArgumentException($"VVE {dto.VveId} not found");
        }

        // If operation plan ID is provided, validate it exists
        if (dto.OperationPlanId.HasValue)
        {
            var plan = await _operationPlanRepository.GetByIdAsync(dto.OperationPlanId.Value);
            if (plan == null)
            {
                throw new ArgumentException($"Operation Plan {dto.OperationPlanId} not found");
            }
        }

        var executedOp = new ExecutedOperation
        {
            Id = Guid.NewGuid(),
            VveId = dto.VveId,
            OperationPlanId = dto.OperationPlanId,
            PlannedOperationIndex = dto.PlannedOperationIndex,
            Type = dto.Type,
            Status = dto.ActualStartTime.HasValue ? ExecutionStatus.Started : ExecutionStatus.Started,
            PlannedStartTime = dto.PlannedStartTime,
            PlannedEndTime = dto.PlannedEndTime,
            ActualStartTime = dto.ActualStartTime,
            ActualEndTime = null,
            ActualResources = dto.ActualResources != null ? SerializeResources(dto.ActualResources) : null,
            UpdatedBy = operatorEmail,
            Notes = dto.Notes
        };

        var created = await _repository.AddAsync(executedOp);
        
        _logger.LogInformation(
            "✅ US 4.1.9: Created executed operation {Id} for VVE {VveId}",
            created.Id, created.VveId);

        // US 4.1.9: Sync ExecutedOperations field in VVE
        await SyncVveExecutedOperationsAsync(created.VveId);

        return MapToDTO(created);
    }

    /// <summary>
    /// US 4.1.9: Update an executed operation with actual times and resources
    /// Automatically determines status based on times
    /// </summary>
    public async Task<ExecutedOperationDTO> UpdateAsync(Guid id, UpdateExecutedOperationDTO dto, string operatorEmail)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new ArgumentException($"Executed operation {id} not found");
        }

        // Update fields if provided
        if (dto.ActualStartTime.HasValue)
            existing.ActualStartTime = dto.ActualStartTime.Value;

        if (dto.ActualEndTime.HasValue)
            existing.ActualEndTime = dto.ActualEndTime.Value;

        if (dto.ActualResources != null)
            existing.ActualResources = SerializeResources(dto.ActualResources);

        if (!string.IsNullOrEmpty(dto.Notes))
            existing.Notes = dto.Notes;

        existing.UpdatedBy = operatorEmail;

        // Determine status automatically or use provided status
        if (!string.IsNullOrEmpty(dto.Status) && 
            Enum.TryParse<ExecutionStatus>(dto.Status, true, out var parsedStatus))
        {
            existing.Status = parsedStatus;
        }
        else
        {
            // Auto-determine status based on times
            if (existing.ActualEndTime.HasValue)
            {
                existing.Status = ExecutionStatus.Completed;
                
                // Check if delayed
                if (existing.PlannedEndTime.HasValue && existing.ActualEndTime.Value > existing.PlannedEndTime.Value)
                {
                    existing.Status = ExecutionStatus.Delayed;
                }
            }
            else if (existing.ActualStartTime.HasValue)
            {
                // Check if currently delayed (past planned end time but not completed)
                if (existing.PlannedEndTime.HasValue && DateTime.UtcNow > existing.PlannedEndTime.Value)
                {
                    existing.Status = ExecutionStatus.Delayed;
                }
                else
                {
                    existing.Status = ExecutionStatus.Started;
                }
            }
        }

        var updated = await _repository.UpdateAsync(existing);
        
        _logger.LogInformation(
            "✅ US 4.1.9: Updated executed operation {Id} with status {Status}",
            updated.Id, updated.Status);

        // US 4.1.9: Sync ExecutedOperations field in VVE
        await SyncVveExecutedOperationsAsync(updated.VveId);

        return MapToDTO(updated);
    }

    /// <summary>
    /// Delete an executed operation
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var operation = await _repository.GetByIdAsync(id);
        if (operation == null)
            return false;

        var vveId = operation.VveId;
        var success = await _repository.DeleteAsync(id);
        
        if (success)
        {
            _logger.LogInformation("Deleted executed operation {Id}", id);
            // US 4.1.9: Sync ExecutedOperations field in VVE
            await SyncVveExecutedOperationsAsync(vveId);
        }
        
        return success;
    }

    /// <summary>
    /// US 4.1.9: Synchronize the ExecutedOperations JSON field in VVE with current executed operations
    /// This ensures the VVE.ExecutedOperations field matches the ExecutedOperation table records
    /// </summary>
    private async Task SyncVveExecutedOperationsAsync(string vveId)
    {
        try
        {
            var vve = await _vveRepository.GetByIdAsync(vveId);
            if (vve == null)
            {
                _logger.LogWarning("Cannot sync ExecutedOperations: VVE {VveId} not found", vveId);
                return;
            }

            // Get all executed operations for this VVE
            var operations = await _repository.GetByVveIdAsync(vveId);
            
            // Convert to simple string array (just operation types)
            var operationTypes = operations.Select(op => op.Type).ToList();

            // Serialize to JSON array of strings
            var json = JsonSerializer.Serialize(operationTypes);
            
            // Update VVE ExecutedOperations field
            vve.ExecutedOperations = json;
            vve.UpdatedAt = DateTime.UtcNow;
            
            // Auto-update ActualBerthTime when first operation starts
            var firstStartedOp = operations.OrderBy(o => o.PlannedOperationIndex).FirstOrDefault(o => o.ActualStartTime.HasValue);
            if (firstStartedOp != null && firstStartedOp.ActualStartTime.HasValue)
            {
                // If BerthTime not set OR is after the operation start, update it
                if (!vve.ActualBerthTime.HasValue || vve.ActualBerthTime.Value > firstStartedOp.ActualStartTime.Value)
                {
                    vve.ActualBerthTime = firstStartedOp.ActualStartTime.Value;
                    _logger.LogInformation("Auto-updated ActualBerthTime to {Time} (first operation started)", vve.ActualBerthTime);
                }
            }
            
            await _vveRepository.UpdateAsync(vve);
            
            _logger.LogInformation(
                "✅ US 4.1.9: Synced ExecutedOperations for VVE {VveId} with {Count} operations",
                vveId, operations.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync ExecutedOperations for VVE {VveId}", vveId);
        }
    }

    /// <summary>
    /// Map domain entity to DTO
    /// </summary>
    private ExecutedOperationDTO MapToDTO(ExecutedOperation eo)
    {
        return new ExecutedOperationDTO
        {
            Id = eo.Id,
            VveId = eo.VveId,
            OperationPlanId = eo.OperationPlanId,
            PlannedOperationIndex = eo.PlannedOperationIndex,
            Type = eo.Type,
            Status = eo.Status.ToString(),
            PlannedStartTime = eo.PlannedStartTime,
            PlannedEndTime = eo.PlannedEndTime,
            ActualStartTime = eo.ActualStartTime,
            ActualEndTime = eo.ActualEndTime,
            ActualResources = DeserializeResources(eo.ActualResources),
            DelayMinutes = eo.DelayMinutes,
            UpdatedBy = eo.UpdatedBy,
            UpdatedAt = eo.UpdatedAt,
            CreatedAt = eo.CreatedAt,
            Notes = eo.Notes
        };
    }

    /// <summary>
    /// Serialize resource list to JSON
    /// </summary>
    private string SerializeResources(List<ResourceUsageDTO> resources)
    {
        return JsonSerializer.Serialize(resources);
    }

    /// <summary>
    /// Deserialize JSON to resource list
    /// </summary>
    private List<ResourceUsageDTO>? DeserializeResources(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<ResourceUsageDTO>>(json);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Helper method to try getting DateTime from JSON element with multiple possible property names
    /// </summary>
    private DateTime? TryGetDateTime(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(prop.GetString(), out var dt))
                        return dt;
                }
                else if (prop.ValueKind == JsonValueKind.Number)
                {
                    try { return prop.GetDateTime(); } catch { }
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// Helper method to try getting int from JSON element with multiple possible property names
    /// </summary>
    private int? TryGetInt(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    try { return prop.GetInt32(); } catch { }
                }
                else if (prop.ValueKind == JsonValueKind.String)
                {
                    if (int.TryParse(prop.GetString(), out var val))
                        return val;
                }
            }
        }
        return null;
    }
}
