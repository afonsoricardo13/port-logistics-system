using Domain.Model;
using Xunit;

namespace Domain.Tests;

public class VesselTypeTests
{
    [Fact]
    public void Create_ValidValues_Succeeds()
    {
        var vt = new VesselType("Panamax", "Common", 4500, 18, 20, 10);
        Assert.Equal("Panamax", vt.Name);
        Assert.Equal(4500, vt.Capacity);
        Assert.Equal(18, vt.MaxRows);
        Assert.Equal(20, vt.MaxBays);
        Assert.Equal(10, vt.MaxTiers);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => new VesselType(name!, "d", 1, 1, 1, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidNumbers_Throws(int invalid)
    {
        Assert.Throws<ArgumentException>(() => new VesselType("N", "d", invalid, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => new VesselType("N", "d", 1, invalid, 1, 1));
        Assert.Throws<ArgumentException>(() => new VesselType("N", "d", 1, 1, invalid, 1));
        Assert.Throws<ArgumentException>(() => new VesselType("N", "d", 1, 1, 1, invalid));
    }

    [Fact]
    public void Update_OnlyProvidedFields_Applied()
    {
        var vt = new VesselType("Panamax", "Common", 4500, 18, 20, 10);
        vt.Update("Updated", 4600, null, 22, null);
        Assert.Equal("Updated", vt.Description);
        Assert.Equal(4600, vt.Capacity);
        Assert.Equal(18, vt.MaxRows); // unchanged
        Assert.Equal(22, vt.MaxBays);
        Assert.Equal(10, vt.MaxTiers); // unchanged
    }
}
