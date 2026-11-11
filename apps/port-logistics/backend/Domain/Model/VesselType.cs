namespace Domain.Model;

public class VesselType
{
    // Persistence Id
    public long Id { get; set; }

    private string _name;
    public string Name => _name;

    private string _description;
    public string Description => _description;

    private int _capacity;
    public int Capacity => _capacity;

    private int _maxRows;
    public int MaxRows => _maxRows;

    private int _maxBays;
    public int MaxBays => _maxBays;

    private int _maxTiers;
    public int MaxTiers => _maxTiers;

    private VesselType()
    {
        _name = string.Empty;
        _description = string.Empty;
        _capacity = 0;
        _maxRows = 0;
        _maxBays = 0;
        _maxTiers = 0;
    }

    public VesselType(string name, string description, int capacity, int maxRows, int maxBays, int maxTiers)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required");
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0");
        if (maxRows <= 0 || maxBays <= 0 || maxTiers <= 0)
            throw new ArgumentException("Constraints must be greater than 0");

        _name = name.Trim();
        _description = description?.Trim() ?? string.Empty;
        _capacity = capacity;
        _maxRows = maxRows;
        _maxBays = maxBays;
        _maxTiers = maxTiers;
    }

    public void Update(string? description, int? capacity, int? maxRows, int? maxBays, int? maxTiers)
    {
        if (capacity.HasValue && capacity.Value <= 0)
            throw new ArgumentException("Capacity must be greater than 0");
        if (maxRows.HasValue && maxRows.Value <= 0)
            throw new ArgumentException("MaxRows must be greater than 0");
        if (maxBays.HasValue && maxBays.Value <= 0)
            throw new ArgumentException("MaxBays must be greater than 0");
        if (maxTiers.HasValue && maxTiers.Value <= 0)
            throw new ArgumentException("MaxTiers must be greater than 0");

        if (description != null) _description = description.Trim();
        if (capacity.HasValue) _capacity = capacity.Value;
        if (maxRows.HasValue) _maxRows = maxRows.Value;
        if (maxBays.HasValue) _maxBays = maxBays.Value;
        if (maxTiers.HasValue) _maxTiers = maxTiers.Value;
    }
}
