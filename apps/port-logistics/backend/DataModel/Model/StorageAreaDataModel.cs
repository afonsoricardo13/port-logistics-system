namespace DataModel.Model;

public class StorageAreaDataModel
{
    public long Id { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int MaxCapacityTeu { get; set; }
    public int CurrentOccupancyTeu { get; set; }

    public List<StorageAreaServedDockDataModel> ServedDocks { get; set; } = new();
}

public class StorageAreaServedDockDataModel
{
    public long Id { get; set; }
    public long StorageAreaId { get; set; }
    public string DockCode { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
}
