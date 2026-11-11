namespace Domain.Model;

/// <summary>
/// Records an operational incident affecting vessel visit executions.
/// </summary>
public class Incident
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to incident type
    /// </summary>
    public string IncidentTypeCode { get; set; } = string.Empty;
    
    public IncidentType? IncidentType { get; set; }
    
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// End time (null if incident is still active)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    public IncidentSeverity Severity { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON serialized list of affected VVE IDs
    /// Can affect: all ongoing VVEs, specific VVEs, or upcoming VVEs
    /// </summary>
    public string AffectedVVEs { get; set; } = string.Empty;
    
    public IncidentStatus Status { get; set; }
    
    /// <summary>
    /// User who created/reported the incident
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? ResolvedAt { get; set; }
    
    /// <summary>
    /// Duration in minutes (computed when incident is resolved)
    /// </summary>
    public int? DurationMinutes { get; set; }
}

public enum IncidentStatus
{
    Active,
    Resolved
}
