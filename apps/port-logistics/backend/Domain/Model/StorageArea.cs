namespace Domain.Model;

public class StorageArea
{
    public long Id { get; set; }
    public string Identifier { get; private set; }
    public string Type { get; private set; } // e.g., yard, warehouse
    public string Location { get; private set; }
    public int MaxCapacityTeu { get; private set; }
    public int CurrentOccupancyTeu { get; private set; }
    private readonly List<ServedDock> _servedDocks = new();
    public IReadOnlyCollection<ServedDock> ServedDocks => _servedDocks.AsReadOnly();

    public StorageArea(string identifier, string type, string location, int maxCapacityTeu, int currentOccupancyTeu = 0)
    {
        if (string.IsNullOrWhiteSpace(identifier)) throw new ArgumentException("Identifier is required");
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required");
        if (string.IsNullOrWhiteSpace(location)) throw new ArgumentException("Location is required");
        if (maxCapacityTeu < 0) throw new ArgumentException("Max capacity cannot be negative");
        if (currentOccupancyTeu < 0) throw new ArgumentException("Current occupancy cannot be negative");
        if (currentOccupancyTeu > maxCapacityTeu) throw new ArgumentException("Current occupancy cannot exceed maximum capacity");

        Identifier = identifier.Trim();
        Type = NormalizeType(type);
        Location = location.Trim();
        MaxCapacityTeu = maxCapacityTeu;
        CurrentOccupancyTeu = currentOccupancyTeu;
    }

    public void Update(string? type, string? location, int? maxCapacityTeu, int? currentOccupancyTeu)
    {
        if (!string.IsNullOrWhiteSpace(type)) Type = NormalizeType(type);
        if (!string.IsNullOrWhiteSpace(location)) Location = location!.Trim();
        if (maxCapacityTeu.HasValue)
        {
            if (maxCapacityTeu.Value < 0) throw new ArgumentException("Max capacity cannot be negative");
            if (CurrentOccupancyTeu > maxCapacityTeu.Value) throw new InvalidOperationException("Current occupancy cannot exceed maximum capacity");
            MaxCapacityTeu = maxCapacityTeu.Value;
        }
        if (currentOccupancyTeu.HasValue)
        {
            if (currentOccupancyTeu.Value < 0) throw new ArgumentException("Current occupancy cannot be negative");
            if (currentOccupancyTeu.Value > MaxCapacityTeu) throw new InvalidOperationException("Current occupancy cannot exceed maximum capacity");
            CurrentOccupancyTeu = currentOccupancyTeu.Value;
        }
    }

    public void SetServedDocks(IEnumerable<ServedDock> docks)
    {
        _servedDocks.Clear();
        if (docks == null) return; // null/empty => serves all docks (by convention)
        foreach (var d in docks)
        {
            if (string.IsNullOrWhiteSpace(d.DockCode)) throw new ArgumentException("Dock code is required");
            if (d.DistanceMeters < 0) throw new ArgumentException("Distance must be non-negative");
            _servedDocks.Add(new ServedDock(d.DockCode.Trim(), d.DistanceMeters));
        }
    }

    private static string NormalizeType(string type)
    {
        var t = type.Trim();
        // keep as provided but normalize common values (yard/warehouse)
        if (string.Equals(t, "yard", StringComparison.OrdinalIgnoreCase)) return "Yard";
        if (string.Equals(t, "warehouse", StringComparison.OrdinalIgnoreCase)) return "Warehouse";
        return t;
    }
}

public record ServedDock(string DockCode, double DistanceMeters);
