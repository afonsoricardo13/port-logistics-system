namespace Application.DTO;

using Domain.Model;

public class DockDTO
{
    public string Code { get; set; }
    public string NameOrNumber { get; set; }
    public string Location { get; set; }
    public double Length { get; set; }
    public double Depth { get; set; }
    public double MaxDraft { get; set; }
    public List<string> AllowedVesselTypes { get; set; } = new();

    public DockDTO() {}

    public DockDTO(string code, string nameOrNumber, string location,
                   double length, double depth, double maxDraft,
                   IEnumerable<string> allowedVesselTypes)
    {
        Code = code; NameOrNumber = nameOrNumber; Location = location;
        Length = length; Depth = depth; MaxDraft = maxDraft;
        AllowedVesselTypes = allowedVesselTypes?.ToList() ?? new List<string>();
    }

    public static DockDTO ToDTO(Dock d) => new DockDTO(
        d.Code, d.NameOrNumber, d.Location,
        d.Physical.Length, d.Physical.Depth, d.Physical.MaxDraft,
        d.AllowedVesselTypes
    );

    public static IEnumerable<DockDTO> ToDTO(IEnumerable<Dock> docks)
        => docks.Select(ToDTO);

    public static Dock ToDomain(DockDTO dto)
    {
        return new Dock(dto.Code, dto.NameOrNumber, dto.Location,
                         dto.Length, dto.Depth, dto.MaxDraft,
                         dto.AllowedVesselTypes);
    }

    public static Dock UpdateToDomain(Dock dock, DockDTO dto)
    {
        dock.Update(dto.NameOrNumber, dto.Location, dto.Length, dto.Depth, dto.MaxDraft, dto.AllowedVesselTypes);
        return dock;
    }
}
