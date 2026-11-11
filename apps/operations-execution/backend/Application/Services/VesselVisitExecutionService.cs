using Application.DTO;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services;


/// Application service for Vessel Visit Execution business logic
public class VesselVisitExecutionService
{
    private readonly IVesselVisitExecutionRepository _repository;
    private readonly ILogger<VesselVisitExecutionService> _logger;
    private readonly PortLogisticsClient _portLogisticsClient;

    public VesselVisitExecutionService(
        IVesselVisitExecutionRepository repository,
        ILogger<VesselVisitExecutionService> logger,
        PortLogisticsClient portLogisticsClient)
    {
        _repository = repository;
        _logger = logger;
        _portLogisticsClient = portLogisticsClient;
    }

  
    /// US 4.1.7: Create a new Vessel Visit Execution
    
    public async Task<VesselVisitExecutionDTO> CreateAsync(CreateVveDTO dto, string createdByEmail)
    {
        _logger.LogInformation("US 4.1.7: Creating VVE for VVN {VvnId}", dto.VvnId);

        // Validate required fields
        if (string.IsNullOrEmpty(dto.VvnId))
        {
            throw new ArgumentException("VVN ID is required", nameof(dto.VvnId));
        }

        if (!dto.ActualArrivalTime.HasValue)
        {
            throw new ArgumentException("Actual arrival time is required", nameof(dto.ActualArrivalTime));
        }

        // Create domain entity
        var vve = new VesselVisitExecution
        {
            Id = await _repository.GenerateNextIdAsync(),
            VvnId = dto.VvnId,
            ActualArrivalTime = dto.ActualArrivalTime,
            Notes = dto.Notes,
            CreatedBy = createdByEmail,
            CreatedAt = DateTime.UtcNow,
            Status = VesselVisitExecutionStatus.InProgress
        };

        // Initialize Audit Log
        AppendAuditLog(vve, createdByEmail, "Create", $"Created VVE for VVN {dto.VvnId}");

        // Save to database
        var created = await _repository.AddAsync(vve);
        
        _logger.LogInformation("✅ US 4.1.7: VVE created with ID {Id} for VVN {VvnId}", created.Id, created.VvnId);

        // Return DTO
        return MapToDTO(created);
    }

    /// US 4.1.8: Update VVE with actual berth time and dock
    public async Task<VesselVisitExecutionDTO?> UpdateBerthingAsync(string id, UpdateVveBerthingDTO dto, string updatedBy)
    {
        var vve = await _repository.GetByIdAsync(id);
        if (vve == null) return null;

        if (vve.Status != VesselVisitExecutionStatus.InProgress)
        {
            throw new InvalidOperationException("Can only update berthing for In Progress executions");
        }

        // Validate VVN to get planned dock
        var vvnResult = await _portLogisticsClient.ValidateVvnAsync(vve.VvnId);
        
        // Check for dock discrepancy
        if (vvnResult.IsValid && !string.IsNullOrEmpty(vvnResult.AssignedDockCode))
        {
            if (!string.Equals(dto.DockUsed, vvnResult.AssignedDockCode, StringComparison.OrdinalIgnoreCase))
            {
                var warning = $"[System Warning] Dock discrepancy: Planned {vvnResult.AssignedDockCode}, Used {dto.DockUsed}.";
                vve.Notes = string.IsNullOrEmpty(vve.Notes) ? warning : vve.Notes + "\n" + warning;
            }
        }

        vve.ActualBerthTime = dto.ActualBerthTime;
        vve.DockUsed = dto.DockUsed;
        vve.UpdatedAt = DateTime.UtcNow;
        vve.UpdatedBy = updatedBy;

        AppendAuditLog(vve, updatedBy, "UpdateBerthing", $"Set BerthTime: {dto.ActualBerthTime}, Dock: {dto.DockUsed}");

        await _repository.UpdateAsync(vve);
        return MapToDTO(vve);
    }


    /// <summary>
    /// US 4.1.11: Complete a Vessel Visit Execution
    /// </summary>
    public async Task<VesselVisitExecutionDTO?> CompleteAsync(string id, CompleteVveDTO dto, string completedBy)
    {
        _logger.LogInformation("US 4.1.11: Completing VVE {Id}", id);

        var vve = await _repository.GetByIdAsync(id);
        if (vve == null)
        {
            _logger.LogWarning("VVE with ID {Id} not found", id);
            return null;
        }

        // Validate status
        if (vve.Status == VesselVisitExecutionStatus.Completed)
        {
            throw new InvalidOperationException("Vessel Visit Execution is already completed");
        }

        // Validate required timestamps
        if (!vve.ActualArrivalTime.HasValue)
        {
            throw new InvalidOperationException("Cannot complete VVE: Actual arrival time is missing");
        }

        if (!vve.ActualBerthTime.HasValue)
        {
            throw new InvalidOperationException("Cannot complete VVE: Actual berth time is missing. Please update berthing information first (US 4.1.8)");
        }

        if (!dto.ActualUnberthTime.HasValue)
        {
            throw new ArgumentException("Actual unberth time is required to complete VVE", nameof(dto.ActualUnberthTime));
        }

        if (!dto.ActualDepartureTime.HasValue)
        {
            throw new ArgumentException("Actual departure time is required to complete VVE", nameof(dto.ActualDepartureTime));
        }

        // Validate timestamp sequence
        if (dto.ActualUnberthTime < vve.ActualBerthTime)
        {
            throw new ArgumentException("Unberth time cannot be before berth time");
        }

        if (dto.ActualDepartureTime < dto.ActualUnberthTime)
        {
            throw new ArgumentException("Departure time cannot be before unberth time");
        }

        // TODO US 4.1.11: Validate that all cargo operations are completed
        // This would require integration with cargo/operations tracking system
        // For now, we log a warning if ExecutedOperations is empty
        if (string.IsNullOrEmpty(vve.ExecutedOperations))
        {
            _logger.LogWarning("US 4.1.11: Completing VVE {Id} without recorded operations", id);
        }

        // Update VVE
        vve.ActualUnberthTime = dto.ActualUnberthTime;
        vve.ActualDepartureTime = dto.ActualDepartureTime;
        vve.Status = VesselVisitExecutionStatus.Completed;
        vve.UpdatedAt = DateTime.UtcNow;
        vve.UpdatedBy = completedBy;

        // Add completion notes if provided
        if (!string.IsNullOrEmpty(dto.Notes))
        {
            vve.Notes = string.IsNullOrEmpty(vve.Notes) 
                ? dto.Notes 
                : vve.Notes + "\n[Completion Notes] " + dto.Notes;
        }

        // Audit logging
        var changes = $"Completed VVE. UnberthTime: {dto.ActualUnberthTime:yyyy-MM-dd HH:mm}, DepartureTime: {dto.ActualDepartureTime:yyyy-MM-dd HH:mm}";
        AppendAuditLog(vve, completedBy, "Complete", changes);

        await _repository.UpdateAsync(vve);
        
        _logger.LogInformation("✅ US 4.1.11: VVE {Id} marked as Completed by {User}", id, completedBy);

        return MapToDTO(vve);
    }

    /// <summary>
    /// Get all VVEs
    /// </summary>
    public async Task<IEnumerable<VesselVisitExecutionDTO>> GetAllAsync()
    {
        var vves = await _repository.GetAllAsync();
        return vves.Select(MapToDTO);
    }


    /// Get VVE by ID

    public async Task<VesselVisitExecutionDTO?> GetByIdAsync(string id)
    {
        var vve = await _repository.GetByIdAsync(id);
        return vve != null ? MapToDTO(vve) : null;
    }

   
    /// Get VVEs by VVN ID

    public async Task<IEnumerable<VesselVisitExecutionDTO>> GetByVvnIdAsync(string vvnId)
    {
        var vves = await _repository.GetByVvnIdAsync(vvnId);
        return vves.Select(MapToDTO);
    }

  
    /// Get VVEs by status
   
    public async Task<IEnumerable<VesselVisitExecutionDTO>> GetByStatusAsync(VesselVisitExecutionStatus status)
    {
        var vves = await _repository.GetByStatusAsync(status);
        return vves.Select(MapToDTO);
    }

    /// Get VVEs by date range
       public async Task<IEnumerable<VesselVisitExecutionDTO>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var vves = await _repository.GetByDateRangeAsync(startDate, endDate);
        return vves.Select(MapToDTO);
    }

    /// <summary>
    /// US 4.1.10: Search and list VVEs with execution metrics
    /// </summary>
    public async Task<IEnumerable<VesselVisitExecutionSearchResultDTO>> SearchWithMetricsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? vesselImo = null,
        VesselVisitExecutionStatus? status = null)
    {
        _logger.LogInformation("US 4.1.10: Searching VVEs with filters - StartDate: {StartDate}, EndDate: {EndDate}, Vessel: {Vessel}, Status: {Status}",
            startDate, endDate, vesselImo, status);

        // Get VVEs based on filters
        IEnumerable<VesselVisitExecution> vves;

        if (startDate.HasValue && endDate.HasValue)
        {
            vves = await _repository.GetByDateRangeAsync(startDate.Value, endDate.Value);
        }
        else
        {
            vves = await _repository.GetAllAsync();
        }

        // Apply additional filters
        if (status.HasValue)
        {
            vves = vves.Where(v => v.Status == status.Value);
        }

        // Map to search result DTOs with metrics
        var results = new List<VesselVisitExecutionSearchResultDTO>();
        
        foreach (var vve in vves)
        {
            var result = await MapToSearchResultDTOAsync(vve);
            
            // Filter by vessel IMO if specified
            if (!string.IsNullOrEmpty(vesselImo))
            {
                if (result.VesselImo != null && result.VesselImo.Contains(vesselImo, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(result);
                }
            }
            else
            {
                results.Add(result);
            }
        }

        _logger.LogInformation("✅ US 4.1.10: Found {Count} VVE records matching filters", results.Count);
        
        return results;
    }

    /// <summary>
    /// US 4.1.10: Map VVE to search result DTO with calculated metrics
    /// </summary>
    private async Task<VesselVisitExecutionSearchResultDTO> MapToSearchResultDTOAsync(VesselVisitExecution vve)
    {
        var dto = new VesselVisitExecutionSearchResultDTO
        {
            Id = vve.Id,
            VvnId = vve.VvnId,
            ActualArrivalTime = vve.ActualArrivalTime,
            ActualBerthTime = vve.ActualBerthTime,
            DockUsed = vve.DockUsed,
            ActualUnberthTime = vve.ActualUnberthTime,
            ActualDepartureTime = vve.ActualDepartureTime,
            Status = vve.Status.ToString(),
            CreatedAt = vve.CreatedAt,
            UpdatedAt = vve.UpdatedAt,
            Notes = vve.Notes
        };

        // Try to get VVN data for vessel info and expected times
        try
        {
            var vvnResult = await _portLogisticsClient.ValidateVvnAsync(vve.VvnId);
            if (vvnResult.IsValid && vvnResult.VvnData != null)
            {
                dto.VesselImo = vvnResult.VvnData.VesselImoNumber;
                dto.VesselName = vvnResult.VvnData.VesselName;
                dto.ExpectedArrivalTime = vvnResult.VvnData.EstimatedTimeOfArrival;
                dto.ExpectedDepartureTime = vvnResult.VvnData.EstimatedTimeOfDeparture;
                
                _logger.LogInformation("Successfully loaded VVN data for {VvnId}: Vessel={VesselName}, IMO={VesselImo}", 
                    vve.VvnId, dto.VesselName, dto.VesselImo);
            }
            else
            {
                _logger.LogWarning("VVN validation failed for {VvnId}: {Error}. Will show incomplete data.", 
                    vve.VvnId, vvnResult.ErrorMessage);
                    
                // Set VVN ID as fallback vessel identification
                dto.VesselName = $"VVN-{vve.VvnId}";
                dto.VesselImo = "Unknown";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception fetching VVN data for {VvnId}: {Error}", vve.VvnId, ex.Message);
            
            // Set fallback data
            dto.VesselName = $"VVN-{vve.VvnId}";
            dto.VesselImo = "Error loading data";
        }

        // Calculate execution metrics
        CalculateExecutionMetrics(vve, dto);

        // Count executed operations
        if (!string.IsNullOrEmpty(vve.ExecutedOperations))
        {
            try
            {
                var operations = JsonSerializer.Deserialize<List<object>>(vve.ExecutedOperations);
                dto.ExecutedOperationsCount = operations?.Count ?? 0;
            }
            catch
            {
                dto.ExecutedOperationsCount = 0;
            }
        }

        return dto;
    }

    /// <summary>
    /// US 4.1.10: Calculate execution metrics for a VVE
    /// </summary>
    private void CalculateExecutionMetrics(VesselVisitExecution vve, VesselVisitExecutionSearchResultDTO dto)
    {
        // Total turnaround time: arrival to departure
        if (vve.ActualArrivalTime.HasValue && vve.ActualDepartureTime.HasValue)
        {
            dto.TotalTurnaroundTimeHours = (vve.ActualDepartureTime.Value - vve.ActualArrivalTime.Value).TotalHours;
        }

        // Berth occupancy time: berth to unberth
        if (vve.ActualBerthTime.HasValue && vve.ActualUnberthTime.HasValue)
        {
            dto.BerthOccupancyTimeHours = (vve.ActualUnberthTime.Value - vve.ActualBerthTime.Value).TotalHours;
        }

        // Waiting time for berthing: arrival to berth
        if (vve.ActualArrivalTime.HasValue && vve.ActualBerthTime.HasValue)
        {
            dto.WaitingTimeForBerthingHours = (vve.ActualBerthTime.Value - vve.ActualArrivalTime.Value).TotalHours;
        }

        // Delay in arrival: actual vs expected arrival
        if (vve.ActualArrivalTime.HasValue && dto.ExpectedArrivalTime.HasValue)
        {
            dto.ArrivalDelayHours = (vve.ActualArrivalTime.Value - dto.ExpectedArrivalTime.Value).TotalHours;
        }

        // Delay in departure: actual vs expected departure
        if (vve.ActualDepartureTime.HasValue && dto.ExpectedDepartureTime.HasValue)
        {
            dto.DepartureDelayHours = (vve.ActualDepartureTime.Value - dto.ExpectedDepartureTime.Value).TotalHours;
        }
    }

    
    /// Map domain entity to DTO
       private VesselVisitExecutionDTO MapToDTO(VesselVisitExecution vve)
    {
        return new VesselVisitExecutionDTO
        {
            Id = vve.Id,
            VvnId = vve.VvnId,
            ActualArrivalTime = vve.ActualArrivalTime,
            ActualBerthTime = vve.ActualBerthTime,
            DockUsed = vve.DockUsed,
            ExecutedOperations = vve.ExecutedOperations,
            ActualUnberthTime = vve.ActualUnberthTime,
            ActualDepartureTime = vve.ActualDepartureTime,
            Status = vve.Status.ToString(),
            CreatedBy = vve.CreatedBy,
            CreatedAt = vve.CreatedAt,
            UpdatedBy = vve.UpdatedBy,
            UpdatedAt = vve.UpdatedAt,
            AuditLog = vve.AuditLog,
            Notes = vve.Notes
        };
    }

    private record AuditEntry(DateTime Timestamp, string User, string Action, string Changes);

    private void AppendAuditLog(VesselVisitExecution vve, string user, string action, string changes)
    {
        var logs = new List<AuditEntry>();
        if (!string.IsNullOrEmpty(vve.AuditLog))
        {
            try
            {
                logs = JsonSerializer.Deserialize<List<AuditEntry>>(vve.AuditLog) ?? new List<AuditEntry>();
            }
            catch
            {
                logs = new List<AuditEntry>(); 
            }
        }

        logs.Add(new AuditEntry(DateTime.UtcNow, user, action, changes));
        vve.AuditLog = JsonSerializer.Serialize(logs);
    }
}
