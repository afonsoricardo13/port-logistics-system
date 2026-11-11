namespace Domain.Tests;

using Domain.Model;

public class DockTest
{
    [Fact]
    public void CreateDock_WithValidData_Succeeds()
    {
        var d = new Dock("D1", "Dock 1", "North", 100, 12, 10, new []{"Container", "Bulk"});
        Assert.Equal("D1", d.Code);
        Assert.Equal("Dock 1", d.NameOrNumber);
        Assert.Equal("North", d.Location);
        Assert.Equal(100, d.Physical.Length);
        Assert.Contains("Container", d.AllowedVesselTypes);
    }

    [Fact]
    public void CreateDock_InvalidSizes_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Dock("D1", "Dock 1", "North", 0, 12, 10, new []{"Container"}));
    }

    [Fact]
    public void UpdateDock_ChangesApplied()
    {
        var d = new Dock("D2", "D2", "West", 50, 8, 7, new []{"RoRo"});
        d.Update("D2-New", "West-Zone", 60, 9, 8, new []{"RoRo", "Tanker"});
        Assert.Equal("D2-New", d.NameOrNumber);
        Assert.Equal(60, d.Physical.Length);
        Assert.Contains("Tanker", d.AllowedVesselTypes);
    }
}
