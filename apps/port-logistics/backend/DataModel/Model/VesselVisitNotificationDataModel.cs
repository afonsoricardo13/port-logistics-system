namespace DataModel.Model;

public class VesselVisitNotificationDataModel
{
    public long Id { get; set; }
    public string VesselImoNumber { get; set; } = string.Empty;
    public DateTime? EstimatedTimeOfArrival { get; set; }
    public DateTime? EstimatedTimeOfDeparture { get; set; }
    public int Status { get; set; }
    public string? AssignedDockCode { get; set; }
    public long? SubmittedByRepresentativeId { get; set; }
    public string DecisionLogsJson { get; set; } = "[]";
    
    // US 2.2.8: Additional notification details
    public string? CaptainName { get; set; }
    public int? CrewCount { get; set; }
    public string? SafetyOfficersJson { get; set; }  // JSON array of safety officer names
    public string? PreferredDockCode { get; set; }
    public string? UnloadingManifestJson { get; set; }  // JSON array of containers to unload
    public string? LoadingManifestJson { get; set; }    // JSON array of containers to load

    public ICollection<ContainerItemDataModel> CargoManifest { get; set; } = new List<ContainerItemDataModel>();
    public ICollection<CrewMemberDataModel> Crew { get; set; } = new List<CrewMemberDataModel>();

    // US 3.4.2: Container counts for scheduling
    public int TotalUnloadingContainers { get; set; }
    public int LoadingContainerCount { get; set; }
}

public class ContainerItemDataModel
{
    public long Id { get; set; }
    public string ContainerId { get; set; } = string.Empty;
    public long VesselVisitNotificationId { get; set; }
}

public class CrewMemberDataModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public long VesselVisitNotificationId { get; set; }
}
