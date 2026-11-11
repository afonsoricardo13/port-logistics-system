namespace Application.DTO;

/// <summary>
/// US 4.1.10: DTO for VVE search results with execution metrics
/// </summary>
public class VesselVisitExecutionSearchResultDTO
{
    /// <summary>
    /// VVE unique identifier
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Reference to Vessel Visit Notification
    /// </summary>
    public string VvnId { get; set; } = string.Empty;
    
    /// <summary>
    /// Vessel IMO number (from VVN data)
    /// </summary>
    public string? VesselImo { get; set; }
    
    /// <summary>
    /// Vessel name (from VVN data)
    /// </summary>
    public string? VesselName { get; set; }
    
    /// <summary>
    /// Actual arrival time at port
    /// </summary>
    public DateTime? ActualArrivalTime { get; set; }
    
    /// <summary>
    /// Actual berth time (when docked)
    /// </summary>
    public DateTime? ActualBerthTime { get; set; }
    
    /// <summary>
    /// Dock actually used
    /// </summary>
    public string? DockUsed { get; set; }
    
    /// <summary>
    /// Actual unberth time (when left dock)
    /// </summary>
    public DateTime? ActualUnberthTime { get; set; }
    
    /// <summary>
    /// Actual departure time from port
    /// </summary>
    public DateTime? ActualDepartureTime { get; set; }
    
    /// <summary>
    /// Execution status
    /// </summary>
    public string Status { get; set; } = "InProgress";
    
    /// <summary>
    /// Expected arrival time (from VVN - for comparison)
    /// </summary>
    public DateTime? ExpectedArrivalTime { get; set; }
    
    /// <summary>
    /// Expected departure time (from VVN - for comparison)
    /// </summary>
    public DateTime? ExpectedDepartureTime { get; set; }
    
    // === EXECUTION METRICS ===
    
    /// <summary>
    /// Total turnaround time (in hours): from arrival to departure
    /// </summary>
    public double? TotalTurnaroundTimeHours { get; set; }
    
    /// <summary>
    /// Berth occupancy time (in hours): from berth to unberth
    /// </summary>
    public double? BerthOccupancyTimeHours { get; set; }
    
    /// <summary>
    /// Waiting time for berthing (in hours): from arrival to berth
    /// </summary>
    public double? WaitingTimeForBerthingHours { get; set; }
    
    /// <summary>
    /// Delay in arrival (in hours): difference between actual and expected arrival
    /// Positive = late, Negative = early
    /// </summary>
    public double? ArrivalDelayHours { get; set; }
    
    /// <summary>
    /// Delay in departure (in hours): difference between actual and expected departure
    /// Positive = late, Negative = early
    /// </summary>
    public double? DepartureDelayHours { get; set; }
    
    /// <summary>
    /// Number of executed operations
    /// </summary>
    public int? ExecutedOperationsCount { get; set; }
    
    /// <summary>
    /// Creation information
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    
    /// <summary>
    /// Last update information
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}
