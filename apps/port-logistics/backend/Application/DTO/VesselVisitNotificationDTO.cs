namespace Application.DTO;

using Domain.Model;

public class CrewMemberDTO
{
    public string Name { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
}

public class VesselVisitNotificationDTO
{
    public long Id { get; set; }
    public string VesselImoNumber { get; set; } = string.Empty;
    public DateTime? EstimatedTimeOfArrival { get; set; }
    public DateTime? EstimatedTimeOfDeparture { get; set; }
    public List<string> ContainerIds { get; set; } = new();
    public List<CrewMemberDTO> Crew { get; set; } = new();
    public string Status { get; set; } = VvnStatus.InProgress.ToString();
    public string? AssignedDockCode { get; set; }
    public long? SubmittedByRepresentativeId { get; set; }
    public string? RejectionReason { get; set; }
    public string? OrganizationIdentifier { get; set; }
    
    // US 2.2.8: Additional fields
    public string? CaptainName { get; set; }
    public int? CrewCount { get; set; }
    public List<string>? SafetyOfficers { get; set; }
    public string? PreferredDockCode { get; set; }
    public object? UnloadingManifest { get; set; }
    public object? LoadingManifest { get; set; }
    
    // US 3.4.2: Container counts for scheduling
    public int TotalUnloadingContainers { get; set; }
    public int LoadingContainerCount { get; set; }

    public static VesselVisitNotificationDTO ToDTO(VesselVisitNotification v)
    {
        return new VesselVisitNotificationDTO
        {
            Id = v.Id,
            VesselImoNumber = v.VesselImoNumber,
            EstimatedTimeOfArrival = v.EstimatedTimeOfArrival,
            EstimatedTimeOfDeparture = v.EstimatedTimeOfDeparture,
            ContainerIds = v.ContainerIds.ToList(),
            Crew = v.Crew.Select(c => new CrewMemberDTO { Name = c.Name, CitizenId = c.CitizenId, Nationality = c.Nationality }).ToList(),
            Status = v.Status.ToString(),
            AssignedDockCode = v.AssignedDockCode,
            SubmittedByRepresentativeId = v.SubmittedByRepresentativeId,
            TotalUnloadingContainers = v.TotalUnloadingContainers,
            LoadingContainerCount = v.LoadingContainerCount
        };
    }

    public static IEnumerable<VesselVisitNotificationDTO> ToDTO(IEnumerable<VesselVisitNotification> list)
        => list.Select(ToDTO);

    public static VesselVisitNotification ToDomain(VesselVisitNotificationDTO dto)
    {
        var crew = dto.Crew.Select(c => new CrewMember(c.Name, c.CitizenId, c.Nationality));
        return new VesselVisitNotification(dto.VesselImoNumber, dto.EstimatedTimeOfArrival, dto.EstimatedTimeOfDeparture,
                                           dto.ContainerIds, crew, dto.SubmittedByRepresentativeId ?? 0);
    }
}

public class DecisionLogDTO
{
    public string OfficerId { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty; // Approved | Rejected
    public DateTime Timestamp { get; set; }
    public string? AssignedDockCode { get; set; }
    public string? Reason { get; set; }
}

public class ApproveNotificationDTO
{
    public string AssignedDockCode { get; set; } = string.Empty;
}

public class RejectNotificationDTO
{
    public string Reason { get; set; } = string.Empty;
}

public class VesselVisitNotificationFilterDTO
{
    public string? VesselImoNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public long? RepresentativeId { get; set; }
    public long? OrganizationId { get; set; }
}
