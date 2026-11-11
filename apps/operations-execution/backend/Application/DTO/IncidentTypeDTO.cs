namespace Application.DTO;

public class CreateIncidentTypeDTO
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? ParentTypeCode { get; set; }
}

public class UpdateIncidentTypeDTO
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? ParentTypeCode { get; set; }
}

public class IncidentTypeDTO
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? ParentTypeCode { get; set; }
    public string? ParentTypeName { get; set; }
    public List<IncidentTypeDTO> ChildTypes { get; set; } = new();
}
