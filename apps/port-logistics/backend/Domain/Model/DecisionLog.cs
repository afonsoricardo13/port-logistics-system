namespace Domain.Model;

public enum DecisionOutcome { Approved, Rejected, DockReassigned }

public class DecisionLog
{
    public string OfficerId { get; set; }
    public DecisionOutcome Outcome { get; set; }
    public DateTime Timestamp { get; set; }
    public string? AssignedDockCode { get; set; }
    public string? Reason { get; set; }
    public string? PreviousDockCode { get; set; } // US 4.3.3 - For tracking reassignments

    public DecisionLog() { OfficerId = string.Empty; Timestamp = DateTime.UtcNow; }

    private DecisionLog(string officerId, DecisionOutcome outcome, string? dockCode, string? reason, string? previousDock = null)
    {
        OfficerId = officerId;
        Outcome = outcome;
        AssignedDockCode = dockCode;
        Reason = reason;
        PreviousDockCode = previousDock;
        Timestamp = DateTime.UtcNow;
    }

    public static DecisionLog Approved(string officerId, string? dockCode)
    {
        if (string.IsNullOrWhiteSpace(officerId)) throw new ArgumentException("OfficerId is required");
        if (string.IsNullOrWhiteSpace(dockCode)) throw new ArgumentException("Assigned dock code is required");
        return new DecisionLog(officerId.Trim(), DecisionOutcome.Approved, dockCode.Trim(), null);
    }

    public static DecisionLog Rejected(string officerId, string reason)
    {
        if (string.IsNullOrWhiteSpace(officerId)) throw new ArgumentException("OfficerId is required");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Rejection reason is required");
        return new DecisionLog(officerId.Trim(), DecisionOutcome.Rejected, null, reason.Trim());
    }

    /// <summary>
    /// US 4.3.3 - Create a dock reassignment log entry
    /// </summary>
    public static DecisionLog DockReassigned(string officerId, string previousDock, string newDock)
    {
        if (string.IsNullOrWhiteSpace(officerId)) throw new ArgumentException("OfficerId is required");
        if (string.IsNullOrWhiteSpace(newDock)) throw new ArgumentException("New dock code is required");
        return new DecisionLog(officerId.Trim(), DecisionOutcome.DockReassigned, newDock.Trim(), 
            $"Dock reassigned from {previousDock} to {newDock} during rebalancing", previousDock);
    }
}
