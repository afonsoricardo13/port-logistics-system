namespace Application.DTO;

/// <summary>
/// US 4.3.3 - Request DTO for dock rebalancing operation
/// </summary>
public class DockRebalanceRequestDTO
{
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD format
    public string OfficerId { get; set; } = string.Empty;
}

/// <summary>
/// US 4.3.3 - Represents a single dock assignment (original or proposed)
/// </summary>
public class VesselDockAssignmentDTO
{
    public long VvnId { get; set; }
    public string VesselImoNumber { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public DateTime EstimatedArrival { get; set; }
    public DateTime EstimatedDeparture { get; set; }
    public string AssignedDock { get; set; } = string.Empty;
    public int ContainerCount { get; set; }
    public double EstimatedDelay { get; set; } // in hours
}

/// <summary>
/// US 4.3.3 - Comparison between current and proposed dock allocations
/// </summary>
public class DockRebalanceProposalDTO
{
    public string Date { get; set; } = string.Empty;
    public List<VesselDockAssignmentDTO> CurrentAllocations { get; set; } = new();
    public List<VesselDockAssignmentDTO> ProposedAllocations { get; set; } = new();
    public DockRebalanceMetricsDTO CurrentMetrics { get; set; } = new();
    public DockRebalanceMetricsDTO ProposedMetrics { get; set; } = new();
    public string RecommendationReason { get; set; } = string.Empty;
}

/// <summary>
/// US 4.3.3 - Performance metrics for dock allocation
/// </summary>
public class DockRebalanceMetricsDTO
{
    public double TotalExpectedDelay { get; set; } // in hours
    public double AverageDelayPerVessel { get; set; } // in hours
    public int VesselsWithDelay { get; set; }
    public Dictionary<string, int> DockUtilization { get; set; } = new(); // dock -> vessel count
    public Dictionary<string, double> DockCongestion { get; set; } = new(); // dock -> congestion score
    public double BalanceScore { get; set; } // 0-100, higher is better
}

/// <summary>
/// US 4.3.3 - Request to confirm rebalancing
/// </summary>
public class DockRebalanceConfirmDTO
{
    public string Date { get; set; } = string.Empty;
    public string OfficerId { get; set; } = string.Empty;
    public List<VesselDockReassignmentDTO> Reassignments { get; set; } = new();
}

/// <summary>
/// US 4.3.3 - Individual reassignment
/// </summary>
public class VesselDockReassignmentDTO
{
    public long VvnId { get; set; }
    public string OriginalDock { get; set; } = string.Empty;
    public string NewDock { get; set; } = string.Empty;
}

/// <summary>
/// US 4.3.3 - Response after applying rebalancing
/// </summary>
public class DockRebalanceResultDTO
{
    public bool Success { get; set; }
    public int ReassignmentsApplied { get; set; }
    public DateTime Timestamp { get; set; }
    public string OfficerId { get; set; } = string.Empty;
    public List<string> Messages { get; set; } = new();
    public DockRebalanceMetricsDTO FinalMetrics { get; set; } = new();
}
