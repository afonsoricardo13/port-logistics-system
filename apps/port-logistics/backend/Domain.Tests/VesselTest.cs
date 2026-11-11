namespace Domain.Tests;

using Domain.Model;

public class VesselTest
{
    [Fact]
    public void CreateVessel_WithValidData_Succeeds()
    {
        var v = new Vessel("9074729", "MSC Oscar", "Container", "Mediterranean Shipping Company");
        Assert.Equal("9074729", v.ImoNumber);
        Assert.Equal("MSC Oscar", v.Name);
        Assert.Equal("Container", v.VesselType);
        Assert.Equal("Mediterranean Shipping Company", v.Operator);
    }

    [Fact]
    public void CreateVessel_WithInvalidImoNumber_Throws()
    {
        // 1234560 would give: 1*7 + 2*6 + 3*5 + 4*4 + 5*3 + 6*2 = 77, 77 % 10 = 7, not 0
        Assert.Throws<ArgumentException>(() => new Vessel("1234560", "Test Ship", "Tanker", "Test Operator"));
    }

    [Fact]
    public void CreateVessel_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Vessel("9074729", "", "Container", "Test Operator"));
    }

    [Fact]
    public void CreateVessel_WithEmptyVesselType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Vessel("9074729", "Test Ship", "", "Test Operator"));
    }

    [Fact]
    public void CreateVessel_WithEmptyOperator_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Vessel("9074729", "Test Ship", "Container", ""));
    }

    [Fact]
    public void UpdateVessel_ChangesApplied()
    {
        var v = new Vessel("9074729", "MSC Oscar", "Container", "MSC");
        v.Update("MSC Oscar II", "Mega Container", "Mediterranean Shipping Company SA");
        Assert.Equal("MSC Oscar II", v.Name);
        Assert.Equal("Mega Container", v.VesselType);
        Assert.Equal("Mediterranean Shipping Company SA", v.Operator);
        Assert.Equal("9074729", v.ImoNumber); // IMO should not change
    }

    [Theory]
    [InlineData("9074729", true)]  // Valid: 9*7 + 0*6 + 7*5 + 4*4 + 7*3 + 2*2 = 63 + 0 + 35 + 16 + 21 + 4 = 139, 139 % 10 = 9
    [InlineData("1234567", true)]  // Valid: 1*7 + 2*6 + 3*5 + 4*4 + 5*3 + 6*2 = 77, 77 % 10 = 7
    [InlineData("IMO9074729", true)]  // Valid with IMO prefix
    [InlineData("1234560", false)] // Invalid check digit - 1*7 + 2*6 + 3*5 + 4*4 + 5*3 + 6*2 = 77, 77%10=7, not 0
    [InlineData("123456", false)]  // Too short
    [InlineData("12345678", false)] // Too long
    [InlineData("ABCDEFG", false)] // Not digits
    [InlineData("", false)]        // Empty
    [InlineData("9074728", false)] // Invalid check digit (should be 9, not 8)
    public void IsValidImoNumber_ValidatesCorrectly(string imoNumber, bool expected)
    {
        Assert.Equal(expected, Vessel.IsValidImoNumber(imoNumber));
    }

    [Fact]
    public void IsValidImoNumber_CalculatesCheckDigitCorrectly()
    {
        // IMO 9074729: Calculate (9*7 + 0*6 + 7*5 + 4*4 + 7*3 + 2*2) % 10 = 139 % 10 = 9 ✓
        Assert.True(Vessel.IsValidImoNumber("9074729"));
        
        // Let's verify with a corrected calculation
        // For IMO to be valid, the 7th digit must equal (sum of first 6 digits * [7,6,5,4,3,2]) % 10
        // Test 1234567: (1*7 + 2*6 + 3*5 + 4*4 + 5*3 + 6*2) % 10 = 77 % 10 = 7 ✓
        Assert.True(Vessel.IsValidImoNumber("1234567"));
    }
}
