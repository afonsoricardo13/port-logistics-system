namespace Domain.Model;

/// <summary>
/// Records non-cargo activities performed during vessel visits 
/// (e.g., inspections, maintenance, cleaning).
/// </summary>
public class ComplementaryTask
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to task category
    /// </summary>
    public string CategoryCode { get; set; } = string.Empty;
    
    public ComplementaryTaskCategory? Category { get; set; }
    
    /// <summary>
    /// Associated vessel visit execution
    /// </summary>
    public string VveId { get; set; } = string.Empty;
    
    public VesselVisitExecution? VesselVisitExecution { get; set; }
    
    /// <summary>
    /// Team or service responsible for the task
    /// </summary>
    public string ResponsibleTeam { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End time (null if task is ongoing)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    public ComplementaryTaskStatus Status { get; set; }
    
    /// <summary>
    /// Impact on cargo operations
    /// </summary>
    public TaskImpactType ImpactType { get; set; }
    
    /// <summary>
    /// Additional notes or details
    /// </summary>
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}

public enum ComplementaryTaskStatus
{
    Ongoing,
    Completed
}

public enum TaskImpactType
{
    /// <summary>
    /// Task runs in parallel with cargo operations
    /// </summary>
    Parallel,
    
    /// <summary>
    /// Task temporarily suspends cargo operations until concluded
    /// </summary>
    Blocking
}
