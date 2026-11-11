namespace DataModel.Model;

using Domain.Model;

public class VesselTypeDataModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int MaxRows { get; set; }
    public int MaxBays { get; set; }
    public int MaxTiers { get; set; }

    public VesselTypeDataModel() {}

    public VesselTypeDataModel(VesselType vt)
    {
        Id = vt.Id;
        Name = vt.Name;
        Description = vt.Description;
        Capacity = vt.Capacity;
        MaxRows = vt.MaxRows;
        MaxBays = vt.MaxBays;
        MaxTiers = vt.MaxTiers;
    }
}
