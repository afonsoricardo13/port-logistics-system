namespace Application.DTO;

/// <summary>
/// DTO for Operation Plan generation request
/// </summary>
public class GenerateOperationPlansRequestDTO
{
    /// <summary>
    /// Target date for which to generate operation plans
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Algorithm to use for scheduling (e.g., "optimal", "heuristic", "multi_crane")
    /// </summary>
    public string Algorithm { get; set; } = "optimal";
    
    /// <summary>
    /// User who is requesting the generation
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
}

/// <summary>
/// DTO for Operation Plan generation response
/// </summary>
public class GenerateOperationPlansResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int PlansGenerated { get; set; }
    public List<OperationPlanSummaryDTO> Plans { get; set; } = new();
    public string? Error { get; set; }
}

/// <summary>
/// Summary DTO for an Operation Plan
/// </summary>
public class OperationPlanSummaryDTO
{
    public Guid Id { get; set; }
    public string VvnId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string AlgorithmUsed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int OperationsCount { get; set; }
    public string? AssignedResources { get; set; }
    
    // Additional fields for US 4.1.3
    public string? VesselImo { get; set; }
    public string? VesselName { get; set; }
    public string? AssignedDock { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Duration { get; set; }
    public double Delay { get; set; }
}

/// <summary>
/// DTO from scheduling service response
/// </summary>
public class SchedulingServiceResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int VesselCount { get; set; }
    public string Algorithm { get; set; } = string.Empty;
    public List<VesselScheduleDTO> Schedule { get; set; } = new();
    public double? ComputationTimeMs { get; set; }
    public double? TotalDelay { get; set; }
}

/// <summary>
/// DTO for individual vessel schedule from Prolog service
/// </summary>
public class VesselScheduleDTO
{
    public string VvnId { get; set; } = string.Empty;
    public string VesselImo { get; set; } = string.Empty;
    public int LoadingTime { get; set; }
    public int UnloadingTime { get; set; }
    public string? AssignedDock { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? Delay { get; set; }
    public List<OperationDetailDTO> Operations { get; set; } = new();
    public List<ResourceAssignmentDTO> Resources { get; set; } = new();
}

/// <summary>
/// DTO for operation details
/// </summary>
public class OperationDetailDTO
{
    public string Type { get; set; } = string.Empty; // "loading" or "unloading"
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Duration { get; set; }
    public string? AssignedCrane { get; set; }
    public string? AssignedDock { get; set; }
}

/// <summary>
/// DTO for resource assignments
/// </summary>
public class ResourceAssignmentDTO
{
    public string Type { get; set; } = string.Empty; // "crane", "dock", "staff"
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; } // Populated from database, not from user input
    public DateTime? AssignedFrom { get; set; }
    public DateTime? AssignedTo { get; set; }
}

/// <summary>
/// US 4.1.5: DTO for VVN without Operation Plan
/// </summary>
public class VvnWithoutPlanDTO
{
    public long Id { get; set; }
    public string VesselImoNumber { get; set; } = string.Empty;
    public string? VesselName { get; set; }
    public DateTime? EstimatedTimeOfArrival { get; set; }
    public DateTime? EstimatedTimeOfDeparture { get; set; }
    public string? AssignedDockCode { get; set; }
}

/// <summary>
/// US 4.1.5: Response DTO for getting VVNs without Operation Plans
/// </summary>
public class VvnsWithoutPlansResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int TotalApprovedVvns { get; set; }
    public int VvnsWithPlans { get; set; }
    public int VvnsWithoutPlans { get; set; }
    public List<VvnWithoutPlanDTO> MissingPlans { get; set; } = new();
}

/// <summary>
/// US 4.1.3: Detailed DTO for Operation Plan with parsed metadata
/// </summary>
public class OperationPlanDetailDTO
{
    public Guid Id { get; set; }
    public string VvnId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string AlgorithmUsed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<string> Operations { get; set; } = new();
    public List<ResourceAssignmentDTO> Resources { get; set; } = new();
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Duration { get; set; }
    public string? VesselImo { get; set; }
    public int LoadingTime { get; set; }
    public int UnloadingTime { get; set; }
    public string? AssignedDock { get; set; }
    public double Delay { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ChangeLogEntryDTO>? ChangeLogs { get; set; }
}

/// <summary>
/// US 4.1.4: Request DTO for manually updating an Operation Plan
/// </summary>
public class UpdateOperationPlanRequestDTO
{
    /// <summary>
    /// User performing the update
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for making this change (required for audit trail)
    /// </summary>
    public string ReasonForChange { get; set; } = string.Empty;
    
    /// <summary>
    /// Updated crane assignments (optional)
    /// </summary>
    public List<ResourceAssignmentDTO>? CraneAssignments { get; set; }
    
    /// <summary>
    /// Updated staff assignments (optional)
    /// </summary>
    public List<ResourceAssignmentDTO>? StaffAssignments { get; set; }
    
    /// <summary>
    /// Updated dock assignment (optional)
    /// </summary>
    public string? AssignedDock { get; set; }
    
    /// <summary>
    /// Updated start time for the operation plan (optional)
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// Updated end time for the operation plan (optional)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Updated operation details (optional)
    /// </summary>
    public List<OperationDetailDTO>? Operations { get; set; }
}

/// <summary>
/// US 4.1.4: Response DTO for operation plan update
/// </summary>
public class UpdateOperationPlanResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public OperationPlanDetailDTO? UpdatedPlan { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<ResourceConflictDTO> ResourceConflicts { get; set; } = new();
    public List<VvnInconsistencyDTO> VvnInconsistencies { get; set; } = new();
}

/// <summary>
/// US 4.1.4: DTO for change log entry
/// </summary>
public class ChangeLogEntryDTO
{
    public DateTime Timestamp { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<string> Changes { get; set; } = new();
}

/// <summary>
/// US 4.1.4: DTO for resource conflicts
/// </summary>
public class ResourceConflictDTO
{
    public string ResourceType { get; set; } = string.Empty; // "crane", "staff", "dock"
    public string ResourceId { get; set; } = string.Empty;
    public string ConflictDescription { get; set; } = string.Empty;
    public List<string> ConflictingVvnIds { get; set; } = new();
    public DateTime ConflictStartTime { get; set; }
    public DateTime ConflictEndTime { get; set; }
}

/// <summary>
/// US 4.1.4: DTO for VVN inconsistencies
/// </summary>
public class VvnInconsistencyDTO
{
    public string VvnId { get; set; } = string.Empty;
    public string InconsistencyType { get; set; } = string.Empty; // "timing", "resource", "capacity"
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "warning", "error"
}

/// <summary>
/// US 4.1.3: Request DTO for searching and listing Operation Plans
/// </summary>
public class SearchOperationPlansRequestDTO
{
    /// <summary>
    /// Start date for the search range
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// End date for the search range
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Filter by vessel IMO number
    /// </summary>
    public string? VesselImo { get; set; }
    
    /// <summary>
    /// Filter by VVN identifier
    /// </summary>
    public string? VvnId { get; set; }
    
    /// <summary>
    /// Sort field (e.g., "StartTime", "VesselName", "Delay")
    /// </summary>
    public string? SortBy { get; set; }
    
    /// <summary>
    /// Sort direction ("asc" or "desc")
    /// </summary>
    public string? SortDirection { get; set; }
    
    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int? PageNumber { get; set; }
    
    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int? PageSize { get; set; }
}

/// <summary>
/// US 4.1.3: Response DTO for operation plan search results
/// </summary>
public class SearchOperationPlansResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalResults { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public int? TotalPages { get; set; }
    public List<OperationPlanSummaryDTO> Plans { get; set; } = new();
}

/// <summary>
/// US 4.1.6: Request DTO for Resource Usage query
/// </summary>
public class ResourceUsageRequestDTO
{
    public string ResourceType { get; set; } = string.Empty; // "crane", "dock", "staff"
    public string ResourceId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// US 4.1.6: DTO for Resource Usage plan entry
/// </summary>
public class ResourceUsagePlanEntryDTO
{
    public Guid PlanId { get; set; }
    public string VvnId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int AllocatedMinutes { get; set; }
    public int OperationsCount { get; set; }
}

/// <summary>
/// US 4.1.6: Response DTO for Resource Usage query
/// </summary>
public class ResourceUsageResultDTO
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalAllocatedMinutes { get; set; }
    public int TotalOperations { get; set; }
    public List<ResourceUsagePlanEntryDTO> Plans { get; set; } = new();
}
