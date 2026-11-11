namespace Domain.Model;

public class Dock
{
    // Persistence Id
    public long Id { get; set; }

    // Business unique identifier
    private string _code = string.Empty; // e.g., Dock code/identifier
    public string Code => _code;

    private string _nameOrNumber = string.Empty;
    public string NameOrNumber => _nameOrNumber;

    private string _location = string.Empty;
    public string Location => _location;

    private PhysicalCharacteristics _physical = null!;
    public PhysicalCharacteristics Physical => _physical;

    private List<string> _allowedVesselTypes = new();
    public IReadOnlyCollection<string> AllowedVesselTypes => _allowedVesselTypes.AsReadOnly();

    private Dock() { }

    public Dock(string code, string nameOrNumber, string location,
                double length, double depth, double maxDraft,
                IEnumerable<string> allowedVesselTypes)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required");
        if (string.IsNullOrWhiteSpace(nameOrNumber))
            throw new ArgumentException("Name/Number is required");
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location is required");

        _code = code.Trim();
        _nameOrNumber = nameOrNumber.Trim();
        _location = location.Trim();
        _physical = new PhysicalCharacteristics(length, depth, maxDraft);

        SetAllowedVesselTypes(allowedVesselTypes);
    }

    public void Update(string nameOrNumber, string location,
                       double length, double depth, double maxDraft,
                       IEnumerable<string> allowedVesselTypes)
    {
        if (!string.IsNullOrWhiteSpace(nameOrNumber)) _nameOrNumber = nameOrNumber.Trim();
        if (!string.IsNullOrWhiteSpace(location)) _location = location.Trim();
        // Validate physical characteristics remain positive on update
        if (!_physical.Update(length, depth, maxDraft))
            throw new ArgumentException("Physical characteristics must be positive");
        SetAllowedVesselTypes(allowedVesselTypes);
    }

    private void SetAllowedVesselTypes(IEnumerable<string> types)
    {
        _allowedVesselTypes = (types ?? Enumerable.Empty<string>())
            .Select(t => t?.Trim() ?? string.Empty)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (_allowedVesselTypes.Count == 0)
            throw new ArgumentException("At least one allowed vessel type is required");
    }
}
