namespace Application.DTO;

/// <summary>
/// DTO for creating a new incident
/// </summary>
public class CreateIncidentDTO
{
    public string IncidentTypeCode { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// List of affected VVE IDs. Can be:
    /// - null or empty: affects all ongoing VVEs
    /// - "*": explicit indicator for all VVEs
    /// - List of specific VVE IDs
    /// </summary>
    public List<string>? AffectedVVEIds { get; set; }
}

/// <summary>
/// DTO for updating an existing incident
/// </summary>
public class UpdateIncidentDTO
{
    public string? IncidentTypeCode { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Severity { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public List<string>? AffectedVVEIds { get; set; }
}

/// <summary>
/// DTO for incident response
/// </summary>
public class IncidentDTO
{
    public Guid Id { get; set; }
    public string IncidentTypeCode { get; set; } = string.Empty;
    public string IncidentTypeName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AffectedVVEIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsActive => Status == "Active";
}

/// <summary>
/// DTO for filtering incidents
/// </summary>
public class IncidentFilterDTO
{
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? ActiveOnly { get; set; }
    public string? VesselVisitId { get; set; }
}
