namespace Domain.Model;

public enum VvnStatus { InProgress = 0, Submitted = 1, Approved = 2, Rejected = 3 }

public class CrewMember
{
    public long Id { get; set; }
    public string Name { get; private set; }
    public string CitizenId { get; private set; }
    public string Nationality { get; private set; }

    private CrewMember() { Name = CitizenId = Nationality = string.Empty; }

    public CrewMember(string name, string citizenId, string nationality)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Crew member name is required");
        if (string.IsNullOrWhiteSpace(citizenId)) throw new ArgumentException("Crew member citizen id is required");
        if (string.IsNullOrWhiteSpace(nationality)) throw new ArgumentException("Crew member nationality is required");
        Name = name.Trim();
        CitizenId = citizenId.Trim();
        Nationality = nationality.Trim();
    }
}

public class VesselVisitNotification
{
    // Persistence Id
    public long Id { get; set; }

    private string _vesselImoNumber = string.Empty;
    public string VesselImoNumber => _vesselImoNumber;

    public DateTime? EstimatedTimeOfArrival { get; private set; }
    public DateTime? EstimatedTimeOfDeparture { get; private set; }

    private readonly List<string> _containerIds = new();
    public IReadOnlyCollection<string> ContainerIds => _containerIds.AsReadOnly();

    private readonly List<CrewMember> _crew = new();
    public IReadOnlyCollection<CrewMember> Crew => _crew.AsReadOnly();

    public VvnStatus Status { get; private set; } = VvnStatus.InProgress;

    // US 2.2.7 additions
    public string? AssignedDockCode { get; private set; }

    private readonly List<DecisionLog> _decisionLogs = new();
    public IReadOnlyCollection<DecisionLog> DecisionLogs => _decisionLogs.AsReadOnly();

    // US 2.2.8 - Who submitted this notification
    public long? SubmittedByRepresentativeId { get; private set; }

    // US 3.4.2 - Container counts for scheduling
    public int TotalUnloadingContainers { get; private set; }
    public int LoadingContainerCount { get; private set; }

    private VesselVisitNotification() { }

    public VesselVisitNotification(string vesselImoNumber,
                                   DateTime? eta,
                                   DateTime? etd,
                                   IEnumerable<string>? containerIds,
                                   IEnumerable<CrewMember>? crew,
                                   long representativeId)
    {
        if (string.IsNullOrWhiteSpace(vesselImoNumber)) throw new ArgumentException("Vessel IMO number is required");
        if (representativeId <= 0) throw new ArgumentException("Representative ID is required");
        if (eta.HasValue && etd.HasValue && etd.Value <= eta.Value)
            throw new ArgumentException("Estimated Time of Departure must be after Estimated Time of Arrival");
        
        _vesselImoNumber = vesselImoNumber.Trim();
        EstimatedTimeOfArrival = eta;
        EstimatedTimeOfDeparture = etd;
        SubmittedByRepresentativeId = representativeId;

        if (containerIds != null)
        {
            foreach (var c in containerIds)
            {
                if (!string.IsNullOrWhiteSpace(c))
                {
                    var code = c.Trim().ToUpperInvariant();
                    // Accept codes during IN PROGRESS even if potentially invalid; strict validation at submit
                    _containerIds.Add(code);
                }
            }
        }
        if (crew != null)
        {
            _crew.AddRange(crew);
        }

        Status = VvnStatus.InProgress;
    }

    public void AddOrReplaceCargoManifest(IEnumerable<string> containerIds)
    {
        _containerIds.Clear();
        foreach (var c in containerIds)
        {
            var code = c?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(code)) _containerIds.Add(code!);
        }
    }

    public void AddOrReplaceCrew(IEnumerable<CrewMember> crew)
    {
        _crew.Clear();
        _crew.AddRange(crew);
    }

    // US 2.2.10: Update notification details (only when InProgress)
    public void UpdateDetails(DateTime? eta, DateTime? etd, List<string> errors)
    {
        if (Status != VvnStatus.InProgress)
        {
            errors.Add("Only notifications in 'InProgress' status can be updated");
            return;
        }

        if (eta.HasValue && etd.HasValue && etd.Value <= eta.Value)
        {
            errors.Add("Estimated Time of Departure must be after Estimated Time of Arrival");
            return;
        }

        EstimatedTimeOfArrival = eta;
        EstimatedTimeOfDeparture = etd;
    }

    public void Submit(List<string> errors)
    {
        // Validate completeness for submission
        if (string.IsNullOrWhiteSpace(_vesselImoNumber)) errors.Add("Vessel IMO number is required");
        
        // Validate dates are present
        if (!EstimatedTimeOfArrival.HasValue) errors.Add("Estimated Time of Arrival is required");
        if (!EstimatedTimeOfDeparture.HasValue) errors.Add("Estimated Time of Departure is required");
        
        // TODO: Re-enable when cargo manifest UI is implemented
        // if (_containerIds.Count == 0) errors.Add("Cargo manifest must include at least one container");

        // Validate ISO 6346 format for all containers (basic pattern + check digit placeholder)
        foreach (var cid in _containerIds)
        {
            if (!Iso6346.IsValid(cid))
                errors.Add($"Invalid container ID (ISO 6346): {cid}");
        }

        if (errors.Count > 0) return;

        Status = VvnStatus.Submitted;
    }

    // For mappers only: force status when materializing from persistence
    public void ForceStatus(VvnStatus status)
    {
        Status = status;
    }

    // For mappers only: force decision logs and dock assignment
    public void ForceDecisionLogs(IEnumerable<DecisionLog> logs)
    {
        _decisionLogs.Clear();
        _decisionLogs.AddRange(logs);
    }

    public void ForceAssignedDock(string? dockCode)
    {
        AssignedDockCode = string.IsNullOrWhiteSpace(dockCode) ? null : dockCode!.Trim();
    }

    // US 3.4.2 - Force container counts (for mapper from DataModel)
    public void ForceContainerCounts(int totalUnloadingContainers, int loadingContainerCount)
    {
        TotalUnloadingContainers = totalUnloadingContainers;
        LoadingContainerCount = loadingContainerCount;
    }

    // US 2.2.7 domain behaviors
    public void Approve(string officerId, string assignedDockCode, List<string> errors)
    {
        if (Status != VvnStatus.Submitted)
        {
            errors.Add("Only submitted notifications can be approved");
            return;
        }
        if (string.IsNullOrWhiteSpace(officerId)) errors.Add("OfficerId is required");
        if (string.IsNullOrWhiteSpace(assignedDockCode)) errors.Add("Assigned dock code is required");
        if (errors.Count > 0) return;

        AssignedDockCode = assignedDockCode.Trim();
        var log = DecisionLog.Approved(officerId.Trim(), AssignedDockCode);
        _decisionLogs.Add(log);
        Status = VvnStatus.Approved;
    }

    public void Reject(string officerId, string reason, List<string> errors)
    {
        if (Status != VvnStatus.Submitted)
        {
            errors.Add("Only submitted notifications can be rejected");
            return;
        }
        if (string.IsNullOrWhiteSpace(officerId)) errors.Add("OfficerId is required");
        if (string.IsNullOrWhiteSpace(reason)) errors.Add("Rejection reason is required");
        if (errors.Count > 0) return;

        var log = DecisionLog.Rejected(officerId.Trim(), reason.Trim());
        _decisionLogs.Add(log);
        Status = VvnStatus.Rejected;
    }

    /// <summary>
    /// US 4.3.3 - Update dock assignment for rebalancing
    /// Only approved VVNs can have their dock reassigned
    /// </summary>
    public void UpdateDockAssignment(string newDockCode, string officerId, List<string> errors)
    {
        if (Status != VvnStatus.Approved)
        {
            errors.Add("Only approved notifications can have dock reassignments");
            return;
        }
        if (string.IsNullOrWhiteSpace(newDockCode))
        {
            errors.Add("New dock code is required");
            return;
        }
        if (string.IsNullOrWhiteSpace(officerId))
        {
            errors.Add("Officer ID is required for logging");
            return;
        }

        var oldDock = AssignedDockCode ?? "NONE";
        AssignedDockCode = newDockCode.Trim();
        
        // Log the reassignment
        var log = DecisionLog.DockReassigned(officerId.Trim(), oldDock, AssignedDockCode);
        _decisionLogs.Add(log);
    }
}

public static class Iso6346
{
    // Basic ISO 6346 validation: Format + check digit algorithm.
    // Note: This implementation follows common references. For production, cross-validate against authoritative sources.
    public static bool IsValid(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim().ToUpperInvariant();
        // Format: 4 letters + 7 digits
        if (code.Length != 11) return false;
        for (int i = 0; i < 4; i++) if (code[i] < 'A' || code[i] > 'Z') return false;
        for (int i = 4; i < 11; i++) if (!char.IsDigit(code[i])) return false;

        // Compute check digit
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            int value = CharValue(code[i]);
            int weight = 1 << i; // 2^i
            sum += value * weight;
        }
        int check = sum % 11 % 10;
        int provided = code[10] - '0';
        return check == provided;
    }

    // Exact ISO 6346 letter mapping table
    // A=10, B=12, C=13, D=14, E=15, F=16, G=17, H=18, I=19, J=20,
    // K=21, L=23, M=24, N=25, O=26, P=27, Q=28, R=29, S=30, T=31,
    // U=32, V=34, W=35, X=36, Y=37, Z=38
    private static readonly int[] LetterValues = new int[]
    {
        10, // A
        12, // B
        13, // C
        14, // D
        15, // E
        16, // F
        17, // G
        18, // H
        19, // I
        20, // J
        21, // K
        23, // L
        24, // M
        25, // N
        26, // O
        27, // P
        28, // Q
        29, // R
        30, // S
        31, // T
        32, // U
        34, // V
        35, // W
        36, // X
        37, // Y
        38  // Z
    };

    private static int CharValue(char c)
    {
        if (char.IsDigit(c)) return c - '0';
        if (c >= 'A' && c <= 'Z') return LetterValues[c - 'A'];
        return 0;
    }
}
