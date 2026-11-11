namespace DataModel.Mapper;

using DataModel.Model;
using Domain.Model;
using System.Text.Json;

public class VesselVisitNotificationMapper
{
    public VesselVisitNotificationDataModel ToDataModel(VesselVisitNotification vvn)
    {
        var dm = new VesselVisitNotificationDataModel
        {
            Id = vvn.Id,
            VesselImoNumber = vvn.VesselImoNumber,
            EstimatedTimeOfArrival = vvn.EstimatedTimeOfArrival,
            EstimatedTimeOfDeparture = vvn.EstimatedTimeOfDeparture,
            Status = (int)vvn.Status,
            AssignedDockCode = vvn.AssignedDockCode,
            SubmittedByRepresentativeId = vvn.SubmittedByRepresentativeId,
            DecisionLogsJson = JsonSerializer.Serialize(vvn.DecisionLogs),
            TotalUnloadingContainers = vvn.TotalUnloadingContainers,
            LoadingContainerCount = vvn.LoadingContainerCount
        };
        dm.CargoManifest = vvn.ContainerIds.Select(c => new ContainerItemDataModel { ContainerId = c }).ToList();
        dm.Crew = vvn.Crew.Select(cm => new CrewMemberDataModel { Name = cm.Name, CitizenId = cm.CitizenId, Nationality = cm.Nationality }).ToList();
        return dm;
    }

    public VesselVisitNotification ToDomain(VesselVisitNotificationDataModel dm)
    {
        var crew = dm.Crew?.Select(c => new CrewMember(c.Name, c.CitizenId, c.Nationality)) ?? Enumerable.Empty<CrewMember>();
        var vvn = new VesselVisitNotification(dm.VesselImoNumber, dm.EstimatedTimeOfArrival, dm.EstimatedTimeOfDeparture,
                                              dm.CargoManifest?.Select(ci => ci.ContainerId) ?? Enumerable.Empty<string>(),
                                              crew,
                                              dm.SubmittedByRepresentativeId ?? 0);
        vvn.Id = dm.Id;
        vvn.ForceStatus((VvnStatus)dm.Status);
        if (!string.IsNullOrWhiteSpace(dm.DecisionLogsJson) && dm.DecisionLogsJson != "[]")
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var logs = JsonSerializer.Deserialize<List<DecisionLog>>(dm.DecisionLogsJson, options) ?? new List<DecisionLog>();
                vvn.ForceDecisionLogs(logs);
            }
            catch (JsonException ex)
            {
                // Log error but don't fail - allow VVN to load without decision logs
                Console.WriteLine($"Warning: Failed to deserialize DecisionLogs for VVN {dm.Id}: {ex.Message}");
                Console.WriteLine($"JSON content: {dm.DecisionLogsJson}");
            }
        }
        vvn.ForceAssignedDock(dm.AssignedDockCode);
        vvn.ForceContainerCounts(dm.TotalUnloadingContainers, dm.LoadingContainerCount);
        return vvn;
    }

    public IEnumerable<VesselVisitNotification> ToDomain(IEnumerable<VesselVisitNotificationDataModel> list)
        => list.Select(ToDomain);

    public void UpdateDataModel(VesselVisitNotificationDataModel dm, VesselVisitNotification vvn)
    {
        dm.VesselImoNumber = vvn.VesselImoNumber;
        dm.EstimatedTimeOfArrival = vvn.EstimatedTimeOfArrival;
        dm.EstimatedTimeOfDeparture = vvn.EstimatedTimeOfDeparture;
        dm.Status = (int)vvn.Status;
        dm.AssignedDockCode = vvn.AssignedDockCode;
        dm.SubmittedByRepresentativeId = vvn.SubmittedByRepresentativeId;
        dm.DecisionLogsJson = JsonSerializer.Serialize(vvn.DecisionLogs);
        dm.CargoManifest = vvn.ContainerIds.Select(c => new ContainerItemDataModel { ContainerId = c, VesselVisitNotificationId = dm.Id }).ToList();
        dm.Crew = vvn.Crew.Select(cm => new CrewMemberDataModel { Name = cm.Name, CitizenId = cm.CitizenId, Nationality = cm.Nationality, VesselVisitNotificationId = dm.Id }).ToList();
        dm.TotalUnloadingContainers = vvn.TotalUnloadingContainers;
        dm.LoadingContainerCount = vvn.LoadingContainerCount;
    }
}
