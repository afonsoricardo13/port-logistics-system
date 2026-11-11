using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// Unit tests for PhysicalResourceService
/// US 2.2.8: Register and manage physical resources (cranes, trucks, etc.)
/// Tests validate service layer logic and integration with repository
/// </summary>
public class PhysicalResourceServiceTests
{
    private readonly Mock<IPhysicalResourceRepository> _mockRepository;
    private readonly PhysicalResourceService _service;

    public PhysicalResourceServiceTests()
    {
        _mockRepository = new Mock<IPhysicalResourceRepository>();
        _service = new PhysicalResourceService(_mockRepository.Object);
    }

    #region Helper Methods

    private PhysicalResource CreateValidResource(string code, string description, string type, int capacity)
    {
        return new PhysicalResource(code, description, type, capacity);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenNoResources_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PhysicalResource>());

        // Act
        var result = await _service.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithResources_ShouldReturnAllResources()
    {
        // Arrange
        var resources = new List<PhysicalResource>
        {
            CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50),
            CreateValidResource("TRUCK-001", "Yard Truck 001", "Truck", 40),
            CreateValidResource("FKL-A-01", "Forklift A01", "Forklift", 5)
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(resources);

        // Act
        var result = await _service.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetByCode Tests

    [Fact]
    public async Task GetByCode_WhenResourceExists_ShouldReturnResource()
    {
        // Arrange
        var resource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);
        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(resource);

        // Act
        var result = await _service.GetByCode("STS-A-01");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("STS-A-01", result.Code);
        Assert.Equal("STS Crane A01", result.Description);
        _mockRepository.Verify(r => r.GetByCodeAsync("STS-A-01"), Times.Once);
    }

    [Fact]
    public async Task GetByCode_WhenResourceDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByCodeAsync("NONEXISTENT"))
            .ReturnsAsync((PhysicalResource?)null);

        // Act
        var result = await _service.GetByCode("NONEXISTENT");

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByCodeAsync("NONEXISTENT"), Times.Once);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task Search_WithCodeFilter_ShouldReturnMatchingResources()
    {
        // Arrange
        var resources = new List<PhysicalResource>
        {
            CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50)
        };
        _mockRepository.Setup(r => r.SearchAsync("STS", null, null, null))
            .ReturnsAsync(resources);

        // Act
        var result = await _service.Search("STS", null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("STS-A-01", result.First().Code);
    }

    [Fact]
    public async Task Search_WithMultipleFilters_ShouldReturnMatchingResources()
    {
        // Arrange
        var resources = new List<PhysicalResource>
        {
            CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50)
        };
        _mockRepository.Setup(r => r.SearchAsync("STS", "Crane", "Crane", "Active"))
            .ReturnsAsync(resources);

        // Act
        var result = await _service.Search("STS", "Crane", "Crane", "Active");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task Search_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.SearchAsync("NONEXISTENT", null, null, null))
            .ReturnsAsync(new List<PhysicalResource>());

        // Act
        var result = await _service.Search("NONEXISTENT", null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Add Tests - Valid Cases

    [Fact]
    public async Task Add_WithValidData_ShouldCreateResource()
    {
        // Arrange
        var dto = new PhysicalResourceDTO
        {
            Code = "STS-A-01",
            Description = "STS Crane A01",
            Type = "Crane",
            Capacity = 50,
            AssignedArea = "Terminal A",
            SetupTimeMinutes = 30
        };

        _mockRepository.Setup(r => r.Exists("STS-A-01"))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.Add(It.IsAny<PhysicalResource>()))
            .ReturnsAsync((PhysicalResource res) => res);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("STS-A-01", result.Code);
        Assert.Equal("STS Crane A01", result.Description);
        Assert.Equal(50, result.Capacity);
        Assert.Empty(errorMessages);
        _mockRepository.Verify(r => r.Add(It.IsAny<PhysicalResource>()), Times.Once);
    }

    [Fact]
    public async Task Add_WithMinimumValidData_ShouldCreateResource()
    {
        // Arrange - Only required fields
        var dto = new PhysicalResourceDTO
        {
            Code = "R1",
            Description = "Resource 1",
            Type = "Generic",
            Capacity = 0
        };

        _mockRepository.Setup(r => r.Exists("R1"))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.Add(It.IsAny<PhysicalResource>()))
            .ReturnsAsync((PhysicalResource res) => res);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("R1", result.Code);
        Assert.Equal(0, result.Capacity);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public async Task Add_WithQualificationRequirements_ShouldCreateResource()
    {
        // Arrange
        var dto = new PhysicalResourceDTO
        {
            Code = "STS-A-01",
            Description = "STS Crane A01",
            Type = "Crane",
            Capacity = 50,
            QualificationRequirementIds = new List<string> { "CRANE_OPERATOR", "SAFETY_CERT" }
        };

        _mockRepository.Setup(r => r.Exists("STS-A-01"))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.Add(It.IsAny<PhysicalResource>()))
            .ReturnsAsync((PhysicalResource res) => res);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.QualificationRequirementIds);
        Assert.Equal(2, result.QualificationRequirementIds.Count);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public async Task Add_WithMaximumCapacity_ShouldCreateResource()
    {
        // Arrange
        var dto = new PhysicalResourceDTO
        {
            Code = "HIGH-CAP",
            Description = "High Capacity Resource",
            Type = "Crane",
            Capacity = 999999
        };

        _mockRepository.Setup(r => r.Exists("HIGH-CAP"))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.Add(It.IsAny<PhysicalResource>()))
            .ReturnsAsync((PhysicalResource res) => res);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(999999, result.Capacity);
        Assert.Empty(errorMessages);
    }

    #endregion

    #region Add Tests - Invalid Cases

    [Fact]
    public async Task Add_WithEmptyCode_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new PhysicalResourceDTO
        {
            Code = "",
            Description = "Test Resource",
            Type = "Crane",
            Capacity = 50
        };

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("Code is required", errorMessages[0]);
        _mockRepository.Verify(r => r.Add(It.IsAny<PhysicalResource>()), Times.Never);
    }

    [Fact]
    public async Task Add_WithWhitespaceCode_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new PhysicalResourceDTO
        {
            Code = "   ",
            Description = "Test Resource",
            Type = "Crane",
            Capacity = 50
        };

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("Code is required", errorMessages[0]);
    }

    [Fact]
    public async Task Add_WithDuplicateCode_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new PhysicalResourceDTO
        {
            Code = "STS-A-01",
            Description = "STS Crane A01",
            Type = "Crane",
            Capacity = 50
        };

        _mockRepository.Setup(r => r.Exists("STS-A-01"))
            .ReturnsAsync(true);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Add(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("already exists", errorMessages[0]);
        _mockRepository.Verify(r => r.Add(It.IsAny<PhysicalResource>()), Times.Never);
    }

    #endregion

    #region Update Tests - Valid Cases

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateResource()
    {
        // Arrange
        var existingResource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);
        var dto = new PhysicalResourceDTO
        {
            Description = "STS Crane A01 - Updated",
            Type = "Crane",
            Capacity = 60,
            AssignedArea = "Terminal B",
            SetupTimeMinutes = 45
        };

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(existingResource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync(existingResource);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("STS-A-01", dto, errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
        _mockRepository.Verify(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task Update_ChangingQualifications_ShouldUpdateCorrectly()
    {
        // Arrange
        var existingResource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);
        var dto = new PhysicalResourceDTO
        {
            Description = "STS Crane A01",
            Type = "Crane",
            Capacity = 50,
            QualificationRequirementIds = new List<string> { "NEW_QUAL_1", "NEW_QUAL_2" }
        };

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(existingResource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync(existingResource);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("STS-A-01", dto, errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public async Task Update_ChangingStatus_ShouldUpdateCorrectly()
    {
        // Arrange
        var existingResource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);
        var dto = new PhysicalResourceDTO
        {
            Description = "STS Crane A01",
            Type = "Crane",
            Capacity = 50,
            Status = "UnderMaintenance"
        };

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(existingResource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync(existingResource);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("STS-A-01", dto, errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
    }

    #endregion

    #region Update Tests - Invalid Cases

    [Fact]
    public async Task Update_WithNonExistentResource_ShouldReturnFalseAndAddError()
    {
        // Arrange
        var dto = new PhysicalResourceDTO
        {
            Description = "Updated Resource",
            Type = "Crane",
            Capacity = 50
        };

        _mockRepository.Setup(r => r.GetByCodeAsync("NONEXISTENT"))
            .ReturnsAsync((PhysicalResource?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("NONEXISTENT", dto, errorMessages);

        // Assert
        Assert.False(result);
        Assert.Single(errorMessages);
        Assert.Contains("not found", errorMessages[0]);
        _mockRepository.Verify(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenRepositoryReturnsNull_ShouldReturnFalse()
    {
        // Arrange
        var existingResource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);
        var dto = new PhysicalResourceDTO
        {
            Description = "Updated",
            Type = "Crane",
            Capacity = 50
        };

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(existingResource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync((PhysicalResource?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Update("STS-A-01", dto, errorMessages);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public async Task Deactivate_WithActiveResource_ShouldDeactivate()
    {
        // Arrange
        var resource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(resource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync(resource);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Deactivate("STS-A-01", "Admin", errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
        _mockRepository.Verify(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_WithNonExistentResource_ShouldReturnFalseAndAddError()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByCodeAsync("NONEXISTENT"))
            .ReturnsAsync((PhysicalResource?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Deactivate("NONEXISTENT", "Admin", errorMessages);

        // Assert
        Assert.False(result);
        Assert.Single(errorMessages);
        Assert.Contains("not found", errorMessages[0]);
    }

    [Fact]
    public async Task Deactivate_WithNullChangedBy_ShouldStillDeactivate()
    {
        // Arrange
        var resource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(resource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync(resource);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Deactivate("STS-A-01", null, errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
    }

    #endregion

    #region Reactivate Tests

    [Fact]
    public async Task Reactivate_WithInactiveResource_ShouldReactivate()
    {
        // Arrange
        var resource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);
        resource.Deactivate("Admin");

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(resource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync(resource);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Reactivate("STS-A-01", "Admin", errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
        _mockRepository.Verify(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task Reactivate_WithNonExistentResource_ShouldReturnFalseAndAddError()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByCodeAsync("NONEXISTENT"))
            .ReturnsAsync((PhysicalResource?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Reactivate("NONEXISTENT", "Admin", errorMessages);

        // Assert
        Assert.False(result);
        Assert.Single(errorMessages);
        Assert.Contains("not found", errorMessages[0]);
    }

    [Fact]
    public async Task Reactivate_WithNullChangedBy_ShouldStillReactivate()
    {
        // Arrange
        var resource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);
        resource.Deactivate("Admin");

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(resource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync(resource);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Reactivate("STS-A-01", null, errorMessages);

        // Assert
        Assert.True(result);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public async Task Reactivate_WhenRepositoryUpdateFails_ShouldReturnFalse()
    {
        // Arrange
        var resource = CreateValidResource("STS-A-01", "STS Crane A01", "Crane", 50);
        resource.Deactivate("Admin");

        _mockRepository.Setup(r => r.GetByCodeAsync("STS-A-01"))
            .ReturnsAsync(resource);
        _mockRepository.Setup(r => r.Update(It.IsAny<PhysicalResource>(), It.IsAny<List<string>>()))
            .ReturnsAsync((PhysicalResource?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.Reactivate("STS-A-01", "Admin", errorMessages);

        // Assert
        Assert.False(result);
    }

    #endregion
}
