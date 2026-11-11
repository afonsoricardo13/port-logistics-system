namespace Application.DTO;

using Domain.Model;

public class PhysicalResourceDTO
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? AssignedArea { get; set; }
    public string? Status { get; set; }
    public int? SetupTimeMinutes { get; set; }
    public List<string>? QualificationRequirementIds { get; set; }
    public string? OperationalWindow { get; set; }
    public int? CapacityPerHour { get; set; }
    public double? AverageSpeedKmH { get; set; }
    public DateTime? ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string? OperationType { get; set; }

    public PhysicalResourceDTO() { }

    public PhysicalResourceDTO(string code, string description, string type, int capacity)
    {
        Code = code; Description = description; Type = type; Capacity = capacity;
    }

    public static PhysicalResourceDTO ToDTO(PhysicalResource r)
        => new PhysicalResourceDTO(r.Code, r.Description, r.Type, r.Capacity)
        {
            AssignedArea = r.AssignedArea,
            Status = r.Status.ToString(),
            SetupTimeMinutes = r.SetupTimeMinutes,
            QualificationRequirementIds = r.QualificationRequirementIds?.ToList(),
            OperationalWindow = r.OperationalWindow,
            CapacityPerHour = r.CapacityPerHour,
            AverageSpeedKmH = r.AverageSpeedKmH,
            ChangedAt = r.ChangedAt,
            ChangedBy = r.ChangedBy,
            OperationType = r.OperationType
        };

    public static IEnumerable<PhysicalResourceDTO> ToDTO(IEnumerable<PhysicalResource> items)
        => items.Select(ToDTO);

    public static PhysicalResource ToDomain(PhysicalResourceDTO dto)
    {
        var resource = new PhysicalResource(dto.Code, dto.Description, dto.Type, dto.Capacity,
            dto.AssignedArea, dto.Status != null ? Enum.Parse<PhysicalResourceStatus>(dto.Status) : PhysicalResourceStatus.Active,
            dto.SetupTimeMinutes, dto.QualificationRequirementIds,
            dto.OperationalWindow, dto.CapacityPerHour, dto.AverageSpeedKmH);
        
        resource.ChangedAt = DateTime.UtcNow;
        resource.ChangedBy = dto.ChangedBy ?? "System";
        resource.OperationType = "Create";
        
        return resource;
    }

    public static PhysicalResource UpdateToDomain(PhysicalResource domain, PhysicalResourceDTO dto)
    {
        var status = dto.Status != null ? Enum.Parse<PhysicalResourceStatus>(dto.Status) : (PhysicalResourceStatus?)null;
        domain.Update(dto.Description, dto.Type, dto.Capacity, dto.AssignedArea, status, dto.SetupTimeMinutes, 
            dto.QualificationRequirementIds, dto.OperationalWindow, dto.CapacityPerHour, dto.AverageSpeedKmH, dto.ChangedBy, "Update");
        return domain;
    }
}
