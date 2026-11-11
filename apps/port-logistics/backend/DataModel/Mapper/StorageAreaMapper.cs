using DataModel.Model;
using Domain.Model;

namespace DataModel.Mapper;

public class StorageAreaMapper
{
    public StorageArea ToDomain(StorageAreaDataModel dm)
    {
        var domain = new StorageArea(dm.Identifier, dm.Type, dm.Location, dm.MaxCapacityTeu, dm.CurrentOccupancyTeu)
        {
            Id = dm.Id
        };
        if (dm.ServedDocks != null && dm.ServedDocks.Count > 0)
        {
            domain.SetServedDocks(dm.ServedDocks.Select(sd => new ServedDock(sd.DockCode, sd.DistanceMeters)));
        }
        return domain;
    }

    public IEnumerable<StorageArea> ToDomain(IEnumerable<StorageAreaDataModel> list)
        => list.Select(ToDomain);

    public StorageAreaDataModel ToDataModel(StorageArea domain)
    {
        var dm = new StorageAreaDataModel
        {
            Id = domain.Id,
            Identifier = domain.Identifier,
            Type = domain.Type,
            Location = domain.Location,
            MaxCapacityTeu = domain.MaxCapacityTeu,
            CurrentOccupancyTeu = domain.CurrentOccupancyTeu,
            ServedDocks = domain.ServedDocks.Select(sd => new StorageAreaServedDockDataModel
            {
                DockCode = sd.DockCode,
                DistanceMeters = sd.DistanceMeters
            }).ToList()
        };
        return dm;
    }

    public void UpdateDataModel(StorageAreaDataModel dm, StorageArea domain)
    {
        dm.Identifier = domain.Identifier;
        dm.Type = domain.Type;
        dm.Location = domain.Location;
        dm.MaxCapacityTeu = domain.MaxCapacityTeu;
        dm.CurrentOccupancyTeu = domain.CurrentOccupancyTeu;
        // Replace served docks
        dm.ServedDocks.Clear();
        dm.ServedDocks.AddRange(domain.ServedDocks.Select(sd => new StorageAreaServedDockDataModel
        {
            StorageAreaId = dm.Id,
            DockCode = sd.DockCode,
            DistanceMeters = sd.DistanceMeters
        }));
    }
}
