namespace Domain.Model;

/// <summary>
/// Represents an Operation Plan that stores scheduling results from the Planning & Scheduling module.
/// </summary>
public class OperationPlan
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Reference to the Vessel Visit Notification ID from Port Logistics module
    /// </summary>
    public string VvnId { get; set; } = string.Empty;
    
    public DateTime Date { get; set; }
    
    /// <summary>
    /// JSON serialized list of planned operations (loading/unloading)
    /// </summary>
    public string Operations { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON serialized assigned resources (cranes, docks, staff)
    /// </summary>
    public string AssignedResources { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON serialized time windows for each operation
    /// </summary>
    public string TimeWindows { get; set; } = string.Empty;
    
    /// <summary>
    /// User who created the operation plan
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Algorithm used to generate this plan (e.g., "optimal", "heuristic", "genetic")
    /// </summary>
    public string AlgorithmUsed { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// US 4.1.4: User who last updated the operation plan
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// US 4.1.4: Date/time of last update
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// US 4.1.4: JSON array of change logs tracking all manual updates
    /// Each entry contains: timestamp, author, changes made, reason
    /// </summary>
    public string? ChangeLogs { get; set; }
}
