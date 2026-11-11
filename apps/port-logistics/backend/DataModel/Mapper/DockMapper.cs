namespace DataModel.Mapper;

using DataModel.Model;
using Domain.Factory;
using Domain.Model;

public class DockMapper
{
    private readonly IDockFactory _factory;

    public DockMapper(IDockFactory factory)
    {
        _factory = factory;
    }

    public Dock ToDomain(DockDataModel dm)
    {
        var types = (dm.AllowedVesselTypesCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var d = _factory.NewDock(dm.Code, dm.NameOrNumber, dm.Location, dm.Length, dm.Depth, dm.MaxDraft, types);
        d.Id = dm.Id;
        return d;
    }

    public IEnumerable<Dock> ToDomain(IEnumerable<DockDataModel> items)
        => items.Select(ToDomain);

    public DockDataModel ToDataModel(Dock d) => new DockDataModel(d);

    public void UpdateDataModel(DockDataModel dm, Dock d)
    {
        dm.NameOrNumber = d.NameOrNumber;
        dm.Location = d.Location;
        dm.Length = d.Physical.Length;
        dm.Depth = d.Physical.Depth;
        dm.MaxDraft = d.Physical.MaxDraft;
        dm.AllowedVesselTypesCsv = string.Join(',', d.AllowedVesselTypes);
    }
}
