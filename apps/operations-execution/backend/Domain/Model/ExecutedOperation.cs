namespace Domain.Model;

/// <summary>
/// US 4.1.9: Records actual execution of a planned operation within a Vessel Visit Execution.
/// Tracks progress, timing deviations, and resource usage compared to the original plan.
/// </summary>
public class ExecutedOperation
{
    /// <summary>
    /// Unique identifier for the executed operation
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to the Vessel Visit Execution this operation belongs to
    /// </summary>
    public string VveId { get; set; } = string.Empty;
    
    /// <summary>
    /// Reference to the Operation Plan this execution is based on
    /// </summary>
    public Guid? OperationPlanId { get; set; }
    
    /// <summary>
    /// Index of the planned operation in the operation plan (0-based)
    /// Used to match executed operation to its corresponding planned operation
    /// </summary>
    public int PlannedOperationIndex { get; set; }
    
    /// <summary>
    /// Type of operation: "loading" or "unloading"
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Current execution status
    /// </summary>
    public ExecutionStatus Status { get; set; }
    
    /// <summary>
    /// Originally planned start time from the operation plan
    /// </summary>
    public DateTime? PlannedStartTime { get; set; }
    
    /// <summary>
    /// Originally planned end time from the operation plan
    /// </summary>
    public DateTime? PlannedEndTime { get; set; }
    
    /// <summary>
    /// Actual start time recorded by operator
    /// </summary>
    public DateTime? ActualStartTime { get; set; }
    
    /// <summary>
    /// Actual end time recorded by operator
    /// </summary>
    public DateTime? ActualEndTime { get; set; }
    
    /// <summary>
    /// JSON serialized actual resource usage (cranes, staff, equipment)
    /// Format: [{"type": "crane", "id": "CRANE-001", "name": "Crane 1"}, ...]
    /// </summary>
    public string? ActualResources { get; set; }
    
    /// <summary>
    /// Calculated delay in minutes (negative if ahead of schedule)
    /// Based on: (ActualEndTime - PlannedEndTime) if both are set
    /// </summary>
    public int? DelayMinutes { get; set; }
    
    /// <summary>
    /// Operator who last updated this execution record
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp of last update
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Timestamp when this execution record was first created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Additional notes from the operator
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Execution status enum matching acceptance criteria:
/// - Started: Operation has begun but not completed
/// - Completed: Operation finished successfully
/// - Delayed: Operation is running behind schedule
/// </summary>
public enum ExecutionStatus
{
    Started,
    Completed,
    Delayed
}
