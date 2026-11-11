namespace DataModel.Model;

using Domain.Model;

public class VesselDataModel
{
    public long Id { get; set; }
    public string ImoNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string VesselType { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;

    public VesselDataModel() { }

    public VesselDataModel(Vessel v)
    {
        Id = v.Id;
        ImoNumber = v.ImoNumber;
        Name = v.Name;
        VesselType = v.VesselType;
        Operator = v.Operator;
    }
}
