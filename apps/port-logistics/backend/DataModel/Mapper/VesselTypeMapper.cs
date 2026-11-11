namespace DataModel.Mapper;

using DataModel.Model;
using Domain.Factory;
using Domain.Model;

public class VesselTypeMapper
{
    private readonly IVesselTypeFactory _factory;

    public VesselTypeMapper(IVesselTypeFactory factory)
    {
        _factory = factory;
    }

    public VesselType ToDomain(VesselTypeDataModel dm)
    {
        var vt = _factory.NewVesselType(dm.Name, dm.Description, dm.Capacity, dm.MaxRows, dm.MaxBays, dm.MaxTiers);
        vt.Id = dm.Id;
        return vt;
    }

    public IEnumerable<VesselType> ToDomain(IEnumerable<VesselTypeDataModel> items)
        => items.Select(ToDomain);

    public VesselTypeDataModel ToDataModel(VesselType vt) => new VesselTypeDataModel(vt);

    public void UpdateDataModel(VesselTypeDataModel dm, VesselType vt)
    {
        dm.Description = vt.Description;
        dm.Capacity = vt.Capacity;
        dm.MaxRows = vt.MaxRows;
        dm.MaxBays = vt.MaxBays;
        dm.MaxTiers = vt.MaxTiers;
    }
}
