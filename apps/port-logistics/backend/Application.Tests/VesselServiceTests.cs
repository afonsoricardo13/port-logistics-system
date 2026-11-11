using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// Unit tests for VesselService
/// US 2.2.2: Register and manage vessel records (create, update)
/// Tests validate service layer logic and integration with repository
/// </summary>
public class VesselServiceTests
{
    private readonly Mock<IVesselRepository> _mockVesselRepository;
    private readonly VesselService _service;

    public VesselServiceTests()
    {
        _mockVesselRepository = new Mock<IVesselRepository>();
        _service = new VesselService(_mockVesselRepository.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenNoVessels_ShouldReturnEmptyList()
    {
        // Arrange
        _mockVesselRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Vessel>());

        // Act
        var result = await _service.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockVesselRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithVessels_ShouldReturnAllVessels()
    {
        // Arrange
        var vesselList = new List<Vessel>
        {
            new Vessel("9074729", "Vessel One", "Container Ship", "Operator A"),
            new Vessel("9119567", "Vessel Two", "Cargo", "Operator B"),
            new Vessel("9283239", "Vessel Three", "Tanker", "Operator C")
        };
        _mockVesselRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(vesselList);

        // Act
        var result = await _service.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockVesselRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetByImoNumber Tests

    [Fact]
    public async Task GetByImoNumber_WhenVesselExists_ShouldReturnVessel()
    {
        // Arrange
        var vessel = new Vessel("9074729", "Test Vessel", "Container Ship", "Test Operator");
        _mockVesselRepository.Setup(r => r.GetByImoNumberAsync("9074729"))
            .ReturnsAsync(vessel);

        // Act
        var result = await _service.GetByImoNumber("9074729");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("9074729", result.ImoNumber);
        Assert.Equal("Test Vessel", result.Name);
        _mockVesselRepository.Verify(r => r.GetByImoNumberAsync("9074729"), Times.Once);
    }

    [Fact]
    public async Task GetByImoNumber_WhenVesselDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockVesselRepository.Setup(r => r.GetByImoNumberAsync("9999999"))
            .ReturnsAsync((Vessel?)null);

        // Act
        var result = await _service.GetByImoNumber("9999999");

        // Assert
        Assert.Null(result);
        _mockVesselRepository.Verify(r => r.GetByImoNumberAsync("9999999"), Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task Search_WithImoNumberFilter_ShouldReturnMatchingVessels()
    {
        // Arrange
        var vesselList = new List<Vessel>
        {
            new Vessel("9074729", "Test Vessel", "Container Ship", "Test Operator")
        };
        _mockVesselRepository.Setup(r => r.SearchAsync("9074729", null, null))
            .ReturnsAsync(vesselList);

        // Act
        var result = await _service.Search(imoNumber: "9074729", name: null, operatorName: null);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("9074729", result.First().ImoNumber);
    }

    [Fact]
    public async Task Search_WithNameFilter_ShouldReturnMatchingVessels()
    {
        // Arrange
        var vesselList = new List<Vessel>
        {
            new Vessel("9074729", "Atlantic Voyager", "Container Ship", "Test Operator")
        };
        _mockVesselRepository.Setup(r => r.SearchAsync(null, "Atlantic", null))
            .ReturnsAsync(vesselList);

        // Act
        var result = await _service.Search(imoNumber: null, name: "Atlantic", operatorName: null);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Atlantic Voyager", result.First().Name);
    }

    [Fact]
    public async Task Search_WithOperatorFilter_ShouldReturnMatchingVessels()
    {
        // Arrange
        var vesselList = new List<Vessel>
        {
            new Vessel("9074729", "Vessel One", "Cargo", "Maersk"),
            new Vessel("9119567", "Vessel Two", "Tanker", "Maersk")
        };
        _mockVesselRepository.Setup(r => r.SearchAsync(null, null, "Maersk"))
            .ReturnsAsync(vesselList);

        // Act
        var result = await _service.Search(imoNumber: null, name: null, operatorName: "Maersk");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Search_WithMultipleFilters_ShouldReturnMatchingVessels()
    {
        // Arrange
        var vesselList = new List<Vessel>
        {
            new Vessel("9074729", "Atlantic Voyager", "Container Ship", "Maersk")
        };
        _mockVesselRepository.Setup(r => r.SearchAsync("9074729", "Atlantic", "Maersk"))
            .ReturnsAsync(vesselList);

        // Act
        var result = await _service.Search("9074729", "Atlantic", "Maersk");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task Search_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        _mockVesselRepository.Setup(r => r.SearchAsync("9999999", null, null))
            .ReturnsAsync(new List<Vessel>());

        // Act
        var result = await _service.Search("9999999", null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Add Tests - Valid Cases

    [Fact]
    public async Task Add_WithValidData_ShouldCreateVessel()
    {
        // Arrange
        var dto = new VesselDTO
        {
            ImoNumber = "9074729",
            Name = "New Vessel",
            VesselType = "Container Ship",
            Operator = "Test Operator"
        };

        _mockVesselRepository.Setup(r => r.Exists("9074729"))
            .ReturnsAsync(false);
        _mockVesselRepository.Setup(r => r.Add(It.IsAny<Vessel>()))
            .ReturnsAsync((Vessel v) => v);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("9074729", result.ImoNumber);
        Assert.Equal("New Vessel", result.Name);
        Assert.Empty(errorMessages);
        _mockVesselRepository.Verify(r => r.Add(It.IsAny<Vessel>()), Times.Once);
    }

    [Fact]
    public async Task Add_WithValidImoNumber_DifferentTypes_ShouldCreateVessels()
    {
        // Arrange
        var cargoDto = new VesselDTO
        {
            ImoNumber = "9119567",
            Name = "Cargo Ship",
            VesselType = "Cargo",
            Operator = "Cargo Operator"
        };

        _mockVesselRepository.Setup(r => r.Exists("9119567"))
            .ReturnsAsync(false);
        _mockVesselRepository.Setup(r => r.Add(It.IsAny<Vessel>()))
            .ReturnsAsync((Vessel v) => v);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(cargoDto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cargo", result.VesselType);
        Assert.Empty(errorMessages);
    }

    #endregion

    #region Add Tests - Invalid Cases

    [Fact]
    public async Task Add_WithDuplicateImoNumber_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new VesselDTO
        {
            ImoNumber = "9074729",
            Name = "Duplicate Vessel",
            VesselType = "Container Ship",
            Operator = "Test Operator"
        };

        _mockVesselRepository.Setup(r => r.Exists("9074729"))
            .ReturnsAsync(true);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("already exists", errorMessages[0]);
        _mockVesselRepository.Verify(r => r.Add(It.IsAny<Vessel>()), Times.Never);
    }

    [Fact]
    public async Task Add_WithInvalidImoNumber_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new VesselDTO
        {
            ImoNumber = "123",
            Name = "Invalid Vessel",
            VesselType = "Container Ship",
            Operator = "Test Operator"
        };

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("Invalid IMO number", errorMessages[0]);
        _mockVesselRepository.Verify(r => r.Exists(It.IsAny<string>()), Times.Never);
        _mockVesselRepository.Verify(r => r.Add(It.IsAny<Vessel>()), Times.Never);
    }

    [Fact]
    public async Task Add_WithInvalidImoCheckDigit_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new VesselDTO
        {
            ImoNumber = "1234560", // Invalid check digit
            Name = "Invalid Vessel",
            VesselType = "Container Ship",
            Operator = "Test Operator"
        };

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("Invalid IMO number", errorMessages[0]);
    }

    #endregion

    #region Update Tests - Valid Cases

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateVessel()
    {
        // Arrange
        var existingVessel = new Vessel("1234567", "Old Name", "Old Type", "Old Operator");
        var dto = new VesselDTO
        {
            ImoNumber = "1234567",
            Name = "Updated Name",
            VesselType = "Updated Type",
            Operator = "Updated Operator"
        };

        _mockVesselRepository.Setup(r => r.GetByImoNumberAsync("1234567"))
            .ReturnsAsync(existingVessel);
        _mockVesselRepository.Setup(r => r.Update(It.IsAny<Vessel>(), It.IsAny<List<string>>()))
            .ReturnsAsync(existingVessel);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("1234567", dto, errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
        _mockVesselRepository.Verify(r => r.Update(It.IsAny<Vessel>(), It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task Update_OnlyName_ShouldUpdateVessel()
    {
        // Arrange
        var existingVessel = new Vessel("9074729", "Old Name", "Container Ship", "Test Operator");
        var dto = new VesselDTO
        {
            ImoNumber = "9074729",
            Name = "New Name",
            VesselType = "Container Ship",
            Operator = "Test Operator"
        };

        _mockVesselRepository.Setup(r => r.GetByImoNumberAsync("9074729"))
            .ReturnsAsync(existingVessel);
        _mockVesselRepository.Setup(r => r.Update(It.IsAny<Vessel>(), It.IsAny<List<string>>()))
            .ReturnsAsync(existingVessel);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("9074729", dto, errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
    }

    #endregion

    #region Update Tests - Invalid Cases

    [Fact]
    public async Task Update_WhenVesselNotFound_ShouldReturnFalseAndAddError()
    {
        // Arrange
        var dto = new VesselDTO
        {
            ImoNumber = "9999999",
            Name = "Non Existent",
            VesselType = "Cargo",
            Operator = "Test Operator"
        };

        _mockVesselRepository.Setup(r => r.GetByImoNumberAsync("9999999"))
            .ReturnsAsync((Vessel?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("9999999", dto, errorMessages);

        // Assert
        Assert.False(result);
        Assert.Single(errorMessages);
        Assert.Contains("not found", errorMessages[0]);
        _mockVesselRepository.Verify(r => r.Update(It.IsAny<Vessel>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenRepositoryFails_ShouldReturnFalse()
    {
        // Arrange
        var existingVessel = new Vessel("9074729", "Old Name", "Old Type", "Old Operator");
        var dto = new VesselDTO
        {
            ImoNumber = "9074729",
            Name = "Updated Name",
            VesselType = "Updated Type",
            Operator = "Updated Operator"
        };

        _mockVesselRepository.Setup(r => r.GetByImoNumberAsync("9074729"))
            .ReturnsAsync(existingVessel);
        _mockVesselRepository.Setup(r => r.Update(It.IsAny<Vessel>(), It.IsAny<List<string>>()))
            .ReturnsAsync((Vessel?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("9074729", dto, errorMessages);

        // Assert
        Assert.False(result);
    }

    #endregion
}
