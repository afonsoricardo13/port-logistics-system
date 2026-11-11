namespace Application.DTO;

using Domain.Model;

public class VesselTypeDTO
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int MaxRows { get; set; }
    public int MaxBays { get; set; }
    public int MaxTiers { get; set; }

    public VesselTypeDTO() {}

    public VesselTypeDTO(string name, string description, int capacity, int maxRows, int maxBays, int maxTiers)
    {
        Name = name;
        Description = description;
        Capacity = capacity;
        MaxRows = maxRows;
        MaxBays = maxBays;
        MaxTiers = maxTiers;
    }

    public static VesselTypeDTO ToDTO(VesselType vt) => new VesselTypeDTO(
        vt.Name, vt.Description, vt.Capacity, vt.MaxRows, vt.MaxBays, vt.MaxTiers
    );

    public static IEnumerable<VesselTypeDTO> ToDTO(IEnumerable<VesselType> items)
        => items.Select(ToDTO);

    public static VesselType ToDomain(VesselTypeDTO dto)
        => new VesselType(dto.Name, dto.Description, dto.Capacity, dto.MaxRows, dto.MaxBays, dto.MaxTiers);

    public static VesselType UpdateToDomain(VesselType vt, VesselTypeDTO dto)
    {
        vt.Update(dto.Description, dto.Capacity, dto.MaxRows, dto.MaxBays, dto.MaxTiers);
        return vt;
    }
}
