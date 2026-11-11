using Domain.Model;
using Xunit;

namespace Domain.Tests;

/// <summary>
/// Unit tests for US 2.2.8: VesselVisitNotification Domain Model
/// Tests validate domain rules for creating and submitting notifications
/// SUT = Domain class (VesselVisitNotification)
/// </summary>
public class VesselVisitNotificationTests_US2_2_8
{
    [Fact]
    public void Create_WithValidData_ShouldCreateInProgressNotification()
    {
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), null, null, 1);
        
        Assert.Equal("IMO1234567", vvn.VesselImoNumber);
        Assert.Equal(VvnStatus.InProgress, vvn.Status);
        Assert.Equal(1, vvn.SubmittedByRepresentativeId);
    }

    [Fact]
    public void Create_WithoutVesselImoNumber_ShouldThrowException()
    {
        Assert.Throws<ArgumentException>(() => 
            new VesselVisitNotification("", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), null, null, 1));
    }

    [Fact]
    public void Create_WithInvalidRepresentativeId_ShouldThrowException()
    {
        Assert.Throws<ArgumentException>(() => 
            new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), null, null, 0));
    }

    [Fact]
    public void Create_WithETDBeforeETA_ShouldThrowException()
    {
        var eta = DateTime.UtcNow.AddDays(7);
        var etd = DateTime.UtcNow.AddDays(6);
        
        Assert.Throws<ArgumentException>(() => 
            new VesselVisitNotification("IMO1234567", eta, etd, null, null, 1));
    }

    [Fact]
    public void Create_WithValidContainers_ShouldStoreContainers()
    {
        var containers = new List<string> { "CSQU3054383", "IJKL5555550" };
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), containers, null, 1);
        
        Assert.Equal(2, vvn.ContainerIds.Count);
        Assert.Contains("CSQU3054383", vvn.ContainerIds);
        Assert.Contains("IJKL5555550", vvn.ContainerIds);
    }

    [Fact]
    public void Create_WithCrew_ShouldStoreCrew()
    {
        var crew = new List<CrewMember> { new CrewMember("John Doe", "PT123", "Portuguese") };
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), null, crew, 1);
        
        Assert.Single(vvn.Crew);
        Assert.Equal("John Doe", vvn.Crew.First().Name);
    }

    [Fact]
    public void Submit_WithValidData_ShouldChangeStatusToSubmitted()
    {
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), 
            new List<string> { "CSQU3054383" }, null, 1);
        var errors = new List<string>();
        
        vvn.Submit(errors);
        
        Assert.Empty(errors);
        Assert.Equal(VvnStatus.Submitted, vvn.Status);
    }

    [Fact]
    public void Submit_WithoutETA_ShouldReturnError()
    {
        var vvn = new VesselVisitNotification("IMO1234567", null, DateTime.UtcNow.AddDays(8), null, null, 1);
        var errors = new List<string>();
        
        vvn.Submit(errors);
        
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Estimated Time of Arrival is required"));
        Assert.Equal(VvnStatus.InProgress, vvn.Status);
    }

    [Fact]
    public void Submit_WithoutETD_ShouldReturnError()
    {
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), null, null, null, 1);
        var errors = new List<string>();
        
        vvn.Submit(errors);
        
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Estimated Time of Departure is required"));
        Assert.Equal(VvnStatus.InProgress, vvn.Status);
    }

    [Fact]
    public void Submit_WithInvalidContainers_ShouldReturnError()
    {
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), 
            new List<string> { "INVALID" }, null, 1);
        var errors = new List<string>();
        
        vvn.Submit(errors);
        
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Invalid container ID (ISO 6346)"));
        Assert.Equal(VvnStatus.InProgress, vvn.Status);
    }

    [Fact]
    public void AddOrReplaceCargoManifest_ShouldReplaceContainers()
    {
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), 
            new List<string> { "CSQU3054383" }, null, 1);
        
        vvn.AddOrReplaceCargoManifest(new List<string> { "IJKL5555550" });
        
        Assert.Single(vvn.ContainerIds);
        Assert.Equal("IJKL5555550", vvn.ContainerIds.First());
    }

    [Fact]
    public void AddOrReplaceCrew_ShouldReplaceCrew()
    {
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), null, 
            new List<CrewMember> { new CrewMember("John", "ID1", "PT") }, 1);
        
        vvn.AddOrReplaceCrew(new List<CrewMember> { new CrewMember("Jane", "ID2", "UK") });
        
        Assert.Single(vvn.Crew);
        Assert.Equal("Jane", vvn.Crew.First().Name);
    }

    [Fact]
    public void UpdateDetails_WhenInProgress_ShouldUpdateDates()
    {
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), null, null, 1);
        var errors = new List<string>();
        var newEta = DateTime.UtcNow.AddDays(10);
        var newEtd = DateTime.UtcNow.AddDays(11);
        
        vvn.UpdateDetails(newEta, newEtd, errors);
        
        Assert.Empty(errors);
        Assert.Equal(newEta, vvn.EstimatedTimeOfArrival);
        Assert.Equal(newEtd, vvn.EstimatedTimeOfDeparture);
    }

    [Fact]
    public void UpdateDetails_WhenNotInProgress_ShouldReturnError()
    {
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), 
            new List<string> { "CSQU3054383" }, null, 1);
        vvn.Submit(new List<string>());
        var errors = new List<string>();
        
        vvn.UpdateDetails(DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(11), errors);
        
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Only notifications in 'InProgress' status can be updated"));
    }

    [Fact]
    public void CrewMember_WithValidData_ShouldCreate()
    {
        var crew = new CrewMember("John Doe", "PT123456", "Portuguese");
        
        Assert.Equal("John Doe", crew.Name);
        Assert.Equal("PT123456", crew.CitizenId);
        Assert.Equal("Portuguese", crew.Nationality);
    }

    [Fact]
    public void CrewMember_WithoutName_ShouldThrowException()
    {
        Assert.Throws<ArgumentException>(() => new CrewMember("", "PT123", "Portuguese"));
    }

    [Fact]
    public void CrewMember_WithoutCitizenId_ShouldThrowException()
    {
        Assert.Throws<ArgumentException>(() => new CrewMember("John", "", "Portuguese"));
    }

    [Fact]
    public void CrewMember_WithoutNationality_ShouldThrowException()
    {
        Assert.Throws<ArgumentException>(() => new CrewMember("John", "PT123", ""));
    }
}
