namespace Domain.Model;

/// <summary>
/// Defines a hierarchical classification system for operational incidents.
/// </summary>
public class IncidentType
{
    /// <summary>
    /// Unique code (e.g., T-INC001)
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public IncidentSeverity Severity { get; set; }
    
    /// <summary>
    /// Parent type code for hierarchical classification (null for root types)
    /// </summary>
    public string? ParentTypeCode { get; set; }
    
    /// <summary>
    /// Navigation property to parent type
    /// </summary>
    public IncidentType? ParentType { get; set; }
    
    /// <summary>
    /// Child types in the hierarchy
    /// </summary>
    public ICollection<IncidentType> ChildTypes { get; set; } = new List<IncidentType>();
}

public enum IncidentSeverity
{
    Minor,
    Major,
    Critical
}
