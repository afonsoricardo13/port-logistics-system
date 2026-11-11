namespace Domain.Model;

/// <summary>
/// Catalog of categories for non-cargo-related activities during vessel visits.
/// </summary>
public class ComplementaryTaskCategory
{
    /// <summary>
    /// Unique code (e.g., CTC001)
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Default duration in minutes (optional)
    /// </summary>
    public int? DefaultDuration { get; set; }
    
    /// <summary>
    /// Expected impact description (e.g., "typically causes 1h delay")
    /// </summary>
    public string? ExpectedImpact { get; set; }
}
