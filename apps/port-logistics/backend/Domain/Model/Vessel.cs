namespace Domain.Model;

public class Vessel
{
    // Persistence Id
    public long Id { get; set; }

    // Business unique identifier - IMO number
    private string _imoNumber;
    public string ImoNumber => _imoNumber;

    private string _name;
    public string Name => _name;

    private string _vesselType;
    public string VesselType => _vesselType;

    private string _operator;
    public string Operator => _operator;

    private Vessel() 
    { 
        _imoNumber = string.Empty;
        _name = string.Empty;
        _vesselType = string.Empty;
        _operator = string.Empty;
    }

    public Vessel(string imoNumber, string name, string vesselType, string operatorName)
    {
        if (!IsValidImoNumber(imoNumber))
            throw new ArgumentException("IMO number must be 7 digits with valid check digit");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vessel name is required");
        if (string.IsNullOrWhiteSpace(vesselType))
            throw new ArgumentException("Vessel type is required");
        if (string.IsNullOrWhiteSpace(operatorName))
            throw new ArgumentException("Operator/Owner is required");

        _imoNumber = imoNumber.Trim();
        _name = name.Trim();
        _vesselType = vesselType.Trim();
        _operator = operatorName.Trim();
    }

    public void Update(string name, string vesselType, string operatorName)
    {
        if (!string.IsNullOrWhiteSpace(name)) _name = name.Trim();
        if (!string.IsNullOrWhiteSpace(vesselType)) _vesselType = vesselType.Trim();
        if (!string.IsNullOrWhiteSpace(operatorName)) _operator = operatorName.Trim();
    }

    /// <summary>
    /// Validates IMO number format: 7 digits where the last digit is a check digit.
    /// IMO format: "IMO" prefix is optional, followed by 7 digits.
    /// Check digit calculation: multiply first 6 digits by 7,6,5,4,3,2 respectively, sum them, and mod 10.
    /// </summary>
    public static bool IsValidImoNumber(string imoNumber)
    {
        if (string.IsNullOrWhiteSpace(imoNumber))
            return false;

        // Remove "IMO" prefix if present
        string digits = imoNumber.Trim().ToUpper();
        if (digits.StartsWith("IMO"))
            digits = digits.Substring(3).Trim();

        // Must be exactly 7 digits
        if (digits.Length != 7 || !digits.All(char.IsDigit))
            return false;

        // Calculate check digit
        int sum = 0;
        int[] multipliers = { 7, 6, 5, 4, 3, 2 };
        
        for (int i = 0; i < 6; i++)
        {
            sum += (digits[i] - '0') * multipliers[i];
        }

        int checkDigit = sum % 10;
        int lastDigit = digits[6] - '0';

        return checkDigit == lastDigit;
    }
}
