namespace Application.DTO;

using Domain.Model;

public class VesselDTO
{
    public string ImoNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VesselType { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;

    public VesselDTO() { }

    public VesselDTO(string imoNumber, string name, string vesselType, string operatorName)
    {
        ImoNumber = imoNumber;
        Name = name;
        VesselType = vesselType;
        Operator = operatorName;
    }

    public static VesselDTO ToDTO(Vessel v) => new VesselDTO(
        v.ImoNumber, v.Name, v.VesselType, v.Operator
    );

    public static IEnumerable<VesselDTO> ToDTO(IEnumerable<Vessel> vessels)
        => vessels.Select(ToDTO);

    public static Vessel ToDomain(VesselDTO dto)
    {
        return new Vessel(dto.ImoNumber, dto.Name, dto.VesselType, dto.Operator);
    }

    public static Vessel UpdateToDomain(Vessel vessel, VesselDTO dto)
    {
        vessel.Update(dto.Name, dto.VesselType, dto.Operator);
        return vessel;
    }
}
