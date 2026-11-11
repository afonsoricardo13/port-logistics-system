namespace Domain.Model;

/// <summary>
/// Records actual vessel visit execution data, tracking what really happened during port operations.
/// </summary>
public class VesselVisitExecution
{
    /// <summary>
    /// Unique identifier following VVN pattern (e.g., VVE-2025-001)
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to the original Vessel Visit Notification
    /// </summary>
    public string VvnId { get; set; } = string.Empty;
    
    /// <summary>
    /// Actual time the vessel arrived at the port
    /// </summary>
    public DateTime? ActualArrivalTime { get; set; }
    
    /// <summary>
    /// Actual time the vessel berthed (docked)
    /// </summary>
    public DateTime? ActualBerthTime { get; set; }
    
    /// <summary>
    /// Dock actually used (may differ from planned)
    /// </summary>
    public string? DockUsed { get; set; }
    
    /// <summary>
    /// JSON serialized list of executed operations with actual start/end times
    /// </summary>
    public string? ExecutedOperations { get; set; }
    
    /// <summary>
    /// Actual time the vessel left the dock
    /// </summary>
    public DateTime? ActualUnberthTime { get; set; }
    
    /// <summary>
    /// Actual time the vessel departed from the port
    /// </summary>
    public DateTime? ActualDepartureTime { get; set; }
    
    public VesselVisitExecutionStatus Status { get; set; }
    
    /// <summary>
    /// User who created this execution record
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// JSON serialized log of all changes for auditability
    /// </summary>
    public string? AuditLog { get; set; }
    
    /// <summary>
    /// Notes or comments about execution
    /// </summary>
    public string? Notes { get; set; }
}

public enum VesselVisitExecutionStatus
{
    InProgress,
    Completed
}
