using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// Unit tests for US 2.2.8: Create/Submit Vessel Visit Notification
/// Tests validate ISO 6346 container validation, crew information, and status management
/// SUT = Application Service (isolated from external dependencies)
/// </summary>
public class VesselVisitNotificationServiceTests_US2_2_8
{
    private readonly Mock<IVesselVisitNotificationRepository> _mockVvnRepo;
    private readonly Mock<IDockRepository> _mockDockRepo;
    private readonly Mock<IVesselRepository> _mockVesselRepo;
    private readonly Mock<IRepresentativeRepository> _mockRepRepo;
    private readonly Mock<IOrganizationRepository> _mockOrgRepo;
    private readonly VesselVisitNotificationService _service;

    public VesselVisitNotificationServiceTests_US2_2_8()
    {
        _mockVvnRepo = new Mock<IVesselVisitNotificationRepository>();
        _mockDockRepo = new Mock<IDockRepository>();
        _mockVesselRepo = new Mock<IVesselRepository>();
        _mockRepRepo = new Mock<IRepresentativeRepository>();
        _mockOrgRepo = new Mock<IOrganizationRepository>();
        
        _service = new VesselVisitNotificationService(
            _mockVvnRepo.Object,
            _mockDockRepo.Object,
            _mockVesselRepo.Object,
            _mockRepRepo.Object,
            _mockOrgRepo.Object);
    }

    #region US 2.2.8 - Create Tests

    [Fact]
    public async Task Create_WithoutVesselImoNumber_ShouldReturnError()
    {
        // Arrange
        var dto = new VesselVisitNotificationDTO
        {
            VesselImoNumber = "",
            EstimatedTimeOfArrival = DateTime.UtcNow.AddDays(7),
            EstimatedTimeOfDeparture = DateTime.UtcNow.AddDays(8)
        };
        var errors = new List<string>();

        // Act
        var result = await _service.Create(dto, errors, 1);

        // Assert
        Assert.Null(result);
        Assert.Contains("Vessel IMO number is required", errors);
    }

    [Fact]
    public async Task Create_WithNonExistentVessel_ShouldReturnError()
    {
        // Arrange
        var dto = new VesselVisitNotificationDTO
        {
            VesselImoNumber = "IMO9999999",
            EstimatedTimeOfArrival = DateTime.UtcNow.AddDays(7),
            EstimatedTimeOfDeparture = DateTime.UtcNow.AddDays(8)
        };
        var errors = new List<string>();

        _mockVesselRepo.Setup(r => r.GetByImoNumberAsync("IMO9999999")).ReturnsAsync((Vessel?)null);

        // Act
        var result = await _service.Create(dto, errors, 1);

        // Assert
        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("does not exist"));
    }

    [Fact]
    public async Task Create_WithInvalidRepresentativeId_ShouldReturnError()
    {
        // Arrange
        var dto = new VesselVisitNotificationDTO
        {
            VesselImoNumber = "IMO1234567",
            EstimatedTimeOfArrival = DateTime.UtcNow.AddDays(7),
            EstimatedTimeOfDeparture = DateTime.UtcNow.AddDays(8)
        };
        var errors = new List<string>();

        // Act
        var result = await _service.Create(dto, errors, 0);

        // Assert
        Assert.Null(result);
        Assert.Contains("Representative ID is required", errors);
    }

    [Fact]
    public async Task Create_WithInvalidContainerISO6346_ShouldReturnError()
    {
        // Arrange
        var dto = new VesselVisitNotificationDTO
        {
            VesselImoNumber = "IMO1234567",
            EstimatedTimeOfArrival = DateTime.UtcNow.AddDays(7),
            EstimatedTimeOfDeparture = DateTime.UtcNow.AddDays(8),
            ContainerIds = new List<string> { "INVALID123" }
        };
        var errors = new List<string>();

        var vessel = new Vessel("IMO1234567", "Test Vessel", "Container", "Test Operator");
        _mockVesselRepo.Setup(r => r.GetByImoNumberAsync("IMO1234567")).ReturnsAsync(vessel);

        // Act
        var result = await _service.Create(dto, errors, 1);

        // Assert
        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("Invalid container ID (ISO 6346)"));
    }

    [Fact]
    public async Task Create_WithMultipleInvalidContainers_ShouldReturnAllErrors()
    {
        // Arrange
        var dto = new VesselVisitNotificationDTO
        {
            VesselImoNumber = "IMO1234567",
            EstimatedTimeOfArrival = DateTime.UtcNow.AddDays(7),
            EstimatedTimeOfDeparture = DateTime.UtcNow.AddDays(8),
            ContainerIds = new List<string> { "INVALID1", "BAD2", "WRONG3" }
        };
        var errors = new List<string>();

        var vessel = new Vessel("IMO1234567", "Test Vessel", "Container", "Test Operator");
        _mockVesselRepo.Setup(r => r.GetByImoNumberAsync("IMO1234567")).ReturnsAsync(vessel);

        // Act
        var result = await _service.Create(dto, errors, 1);

        // Assert
        Assert.Null(result);
        Assert.Equal(3, errors.Count);
        Assert.All(errors, e => Assert.Contains("Invalid container ID (ISO 6346)", e));
    }

    [Fact]
    public async Task Create_WithInvalidCrewCount_ShouldReturnError()
    {
        // Arrange
        var dto = new VesselVisitNotificationDTO
        {
            VesselImoNumber = "IMO1234567",
            EstimatedTimeOfArrival = DateTime.UtcNow.AddDays(7),
            EstimatedTimeOfDeparture = DateTime.UtcNow.AddDays(8),
            CrewCount = 0
        };
        var errors = new List<string>();

        var vessel = new Vessel("IMO1234567", "Test Vessel", "Container", "Test Operator");
        _mockVesselRepo.Setup(r => r.GetByImoNumberAsync("IMO1234567")).ReturnsAsync(vessel);

        // Act
        var result = await _service.Create(dto, errors, 1);

        // Assert
        Assert.Null(result);
        Assert.Contains("Crew count must be at least 1", errors);
    }

    [Fact]
    public async Task Create_WithInvalidPreferredDock_ShouldReturnError()
    {
        // Arrange
        var dto = new VesselVisitNotificationDTO
        {
            VesselImoNumber = "IMO1234567",
            EstimatedTimeOfArrival = DateTime.UtcNow.AddDays(7),
            EstimatedTimeOfDeparture = DateTime.UtcNow.AddDays(8),
            PreferredDockCode = "INVALID_DOCK"
        };
        var errors = new List<string>();

        var vessel = new Vessel("IMO1234567", "Test Vessel", "Container", "Test Operator");
        var docks = new List<Dock> { new Dock("DOCK001", "Main Dock", "North Port", 100.0, 15.0, 12.0, new List<string> { "Container Ship" }) };
        
        _mockVesselRepo.Setup(r => r.GetByImoNumberAsync("IMO1234567")).ReturnsAsync(vessel);
        _mockDockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(docks);

        // Act
        var result = await _service.Create(dto, errors, 1);

        // Assert
        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("does not exist"));
    }

    #endregion

    #region US 2.2.8 - Submit Tests

    [Fact]
    public async Task Submit_WhenNotificationNotFound_ShouldReturnError()
    {
        // Arrange
        var errors = new List<string>();
        _mockVvnRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((VesselVisitNotification?)null);

        // Act
        var result = await _service.Submit(999, errors);

        // Assert
        Assert.Null(result);
        Assert.Contains("not found", errors[0]);
    }

    [Fact]
    public async Task Submit_WithMissingETA_ShouldReturnError()
    {
        // Arrange
        var vvn = new VesselVisitNotification("IMO1234567", null, DateTime.UtcNow.AddDays(8), null, null, 1);
        var errors = new List<string>();

        _mockVvnRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vvn);

        // Act
        var result = await _service.Submit(1, errors);

        // Assert
        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("Estimated Time of Arrival is required"));
    }

    [Fact]
    public async Task Submit_WithMissingETD_ShouldReturnError()
    {
        // Arrange
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), null, null, null, 1);
        var errors = new List<string>();

        _mockVvnRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vvn);

        // Act
        var result = await _service.Submit(1, errors);

        // Assert
        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("Estimated Time of Departure is required"));
    }

    [Fact]
    public async Task Submit_WithInvalidContainers_ShouldReturnError()
    {
        // Arrange
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), 
            new List<string> { "INVALID" }, null, 1);
        var errors = new List<string>();

        _mockVvnRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vvn);

        // Act
        var result = await _service.Submit(1, errors);

        // Assert
        Assert.Null(result);
        Assert.Contains(errors, e => e.Contains("Invalid container ID (ISO 6346)"));
    }

    [Fact]
    public async Task Submit_WhenRepositoryUpdateFails_ShouldReturnNull()
    {
        // Arrange
        var vvn = new VesselVisitNotification("IMO1234567", DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(8), 
            new List<string> { "ABCD1234567" }, null, 1);
        var errors = new List<string>();

        _mockVvnRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()))
            .ReturnsAsync((VesselVisitNotification?)null);

        // Act
        var result = await _service.Submit(1, errors);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ISO 6346 Validation Tests

    [Theory]
    [InlineData("CSQU3054383")] // Valid - Real example
    [InlineData("IJKL5555550")] // Valid - Calculated check digit
    public void ISO6346_WithValidContainers_ShouldPass(string containerId)
    {
        // Act & Assert
        Assert.True(Iso6346.IsValid(containerId));
    }

    [Theory]
    [InlineData("INVALID")]     // Too short
    [InlineData("ABCD123456")]  // Wrong length
    [InlineData("1234ABCD567")] // Numbers first
    [InlineData("ABCD12345678")]// Too long
    [InlineData("")]            // Empty
    public void ISO6346_WithInvalidContainers_ShouldFail(string containerId)
    {
        // Act & Assert
        Assert.False(Iso6346.IsValid(containerId));
    }

    #endregion
}
