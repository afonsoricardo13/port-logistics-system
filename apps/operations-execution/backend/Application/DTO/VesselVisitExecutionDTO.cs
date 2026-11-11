namespace Application.DTO;

/// <summary>
/// DTO for Vessel Visit Execution
/// </summary>
public class VesselVisitExecutionDTO
{
    public string? Id { get; set; }
    public required string VvnId { get; set; }
    public DateTime? ActualArrivalTime { get; set; }
    public DateTime? ActualBerthTime { get; set; }
    public string? DockUsed { get; set; }
    public string? ExecutedOperations { get; set; }
    public DateTime? ActualUnberthTime { get; set; }
    public DateTime? ActualDepartureTime { get; set; }
    public string Status { get; set; } = "InProgress";
    public required string CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? AuditLog { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// US 4.1.7: DTO for creating a new Vessel Visit Execution
/// </summary>
public class CreateVveDTO
{
    public required string VvnId { get; set; }
    public DateTime? ActualArrivalTime { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// US 4.1.8: DTO for updating berth information
/// </summary>
public class UpdateBerthDTO
{
    public DateTime? ActualBerthTime { get; set; }
    public string? DockUsed { get; set; }
}

/// <summary>
/// US 4.1.9: DTO for updating operations
/// </summary>
public class UpdateOperationsDTO
{
    public string? ExecutedOperations { get; set; }
}

/// <summary>
/// US 4.1.11: DTO for completing VVE
/// </summary>
public class CompleteVveDTO
{
    public DateTime? ActualUnberthTime { get; set; }
    public DateTime? ActualDepartureTime { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for partial updates to VesselVisitExecution (used by PATCH)
/// </summary>
public class VesselVisitExecutionPatchDto
{
    public Domain.Model.VesselVisitExecutionStatus? Status { get; set; }
    public DateTime? ActualBerthTime { get; set; }
    public DateTime? ActualUnberthTime { get; set; }
    public DateTime? ActualDepartureTime { get; set; }
    public string? DockUsed { get; set; }
    public string? Notes { get; set; }
}
