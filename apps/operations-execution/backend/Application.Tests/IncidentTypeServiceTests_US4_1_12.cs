using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.12: Unit tests for Incident Type Management
/// Tests verify CRUD operations for incident type classification system
/// </summary>
public class IncidentTypeServiceTests_US4_1_12
{
    private readonly Mock<IIncidentTypeRepository> _mockRepository;
    private readonly IncidentTypeService _service;

    public IncidentTypeServiceTests_US4_1_12()
    {
        _mockRepository = new Mock<IIncidentTypeRepository>();
        _service = new IncidentTypeService(_mockRepository.Object);
    }

    #region GetAll Tests

    /// <summary>
    /// Test 1: Verifies GetAllAsync returns all incident types
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllIncidentTypes()
    {
        // Arrange
        var types = new List<IncidentType>
        {
            new IncidentType { Code = "T-INC001", Name = "Equipment Failure", Severity = IncidentSeverity.Major },
            new IncidentType { Code = "T-INC002", Name = "Weather Delay", Severity = IncidentSeverity.Minor }
        };

        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(types);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    /// <summary>
    /// Test 2: Verifies GetAllAsync can filter root types only
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ReturnsRootTypesOnly_WhenRootOnlyIsTrue()
    {
        // Arrange
        var rootTypes = new List<IncidentType>
        {
            new IncidentType { Code = "T-INC001", Name = "Equipment", ParentTypeCode = null }
        };

        _mockRepository.Setup(r => r.GetRootTypesAsync()).ReturnsAsync(rootTypes);

        // Act
        var result = await _service.GetAllAsync(rootOnly: true);

        // Assert
        Assert.Single(result);
        _mockRepository.Verify(r => r.GetRootTypesAsync(), Times.Once);
    }

    /// <summary>
    /// Test 3: Verifies GetAllAsync can filter by parent code
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ReturnsChildTypes_WhenParentCodeProvided()
    {
        // Arrange
        var parentCode = "T-INC001";
        var childTypes = new List<IncidentType>
        {
            new IncidentType { Code = "T-INC001-A", Name = "Crane Failure", ParentTypeCode = parentCode }
        };

        _mockRepository.Setup(r => r.GetChildTypesAsync(parentCode)).ReturnsAsync(childTypes);

        // Act
        var result = await _service.GetAllAsync(parentCode: parentCode);

        // Assert
        Assert.Single(result);
        _mockRepository.Verify(r => r.GetChildTypesAsync(parentCode), Times.Once);
    }

    #endregion

    #region GetByCode Tests

    /// <summary>
    /// Test 4: Verifies GetByCodeAsync returns incident type when exists
    /// </summary>
    [Fact]
    public async Task GetByCodeAsync_ReturnsIncidentType_WhenExists()
    {
        // Arrange
        var code = "T-INC001";
        var incidentType = new IncidentType
        {
            Code = code,
            Name = "Equipment Failure",
            Description = "Mechanical failure of port equipment",
            Severity = IncidentSeverity.Major
        };

        _mockRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync(incidentType);

        // Act
        var result = await _service.GetByCodeAsync(code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(code, result.Code);
        Assert.Equal("Equipment Failure", result.Name);
    }

    /// <summary>
    /// Test 5: Verifies GetByCodeAsync returns null when not exists
    /// </summary>
    [Fact]
    public async Task GetByCodeAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var code = "NONEXISTENT";
        _mockRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync((IncidentType?)null);

        // Act
        var result = await _service.GetByCodeAsync(code);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Test 6: Verifies CreateAsync creates new incident type successfully
    /// </summary>
    [Fact]
    public async Task CreateAsync_CreatesIncidentType_WhenValid()
    {
        // Arrange
        var dto = new CreateIncidentTypeDTO
        {
            Code = "T-INC003",
            Name = "Staff Shortage",
            Description = "Insufficient staff available",
            Severity = "Minor"
        };

        _mockRepository.Setup(r => r.ExistsAsync(dto.Code)).ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<IncidentType>()))
            .ReturnsAsync((IncidentType t) => t);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(dto.Code, result.Code);
        Assert.Equal(dto.Name, result.Name);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<IncidentType>()), Times.Once);
    }

    /// <summary>
    /// Test 7: Verifies CreateAsync throws when code already exists
    /// </summary>
    [Fact]
    public async Task CreateAsync_ThrowsException_WhenCodeExists()
    {
        // Arrange
        var dto = new CreateIncidentTypeDTO { Code = "T-INC001" };
        _mockRepository.Setup(r => r.ExistsAsync(dto.Code)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    /// <summary>
    /// Test 8: Verifies CreateAsync throws when parent code not found
    /// </summary>
    [Fact]
    public async Task CreateAsync_ThrowsException_WhenParentNotFound()
    {
        // Arrange
        var dto = new CreateIncidentTypeDTO
        {
            Code = "T-INC003",
            Name = "Test",
            Severity = "Minor",
            ParentTypeCode = "NONEXISTENT"
        };

        _mockRepository.Setup(r => r.ExistsAsync(dto.Code)).ReturnsAsync(false);
        _mockRepository.Setup(r => r.ExistsAsync(dto.ParentTypeCode)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    /// <summary>
    /// Test 9: Verifies CreateAsync throws for invalid severity
    /// </summary>
    [Fact]
    public async Task CreateAsync_ThrowsException_WhenSeverityInvalid()
    {
        // Arrange
        var dto = new CreateIncidentTypeDTO
        {
            Code = "T-INC003",
            Name = "Test",
            Severity = "InvalidSeverity"
        };

        _mockRepository.Setup(r => r.ExistsAsync(dto.Code)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Test 10: Verifies UpdateAsync updates incident type successfully
    /// </summary>
    [Fact]
    public async Task UpdateAsync_UpdatesIncidentType_WhenValid()
    {
        // Arrange
        var code = "T-INC001";
        var existing = new IncidentType
        {
            Code = code,
            Name = "Old Name",
            Severity = IncidentSeverity.Minor
        };

        var dto = new UpdateIncidentTypeDTO
        {
            Name = "New Name",
            Description = "Updated description",
            Severity = "Major"
        };

        _mockRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync(existing);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<IncidentType>()))
            .ReturnsAsync((IncidentType t) => t);

        // Act
        var result = await _service.UpdateAsync(code, dto);

        // Assert
        Assert.Equal("New Name", result.Name);
        Assert.Equal("Major", result.Severity);
    }

    /// <summary>
    /// Test 11: Verifies UpdateAsync throws when incident type not found
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ThrowsException_WhenNotFound()
    {
        // Arrange
        var code = "NONEXISTENT";
        var dto = new UpdateIncidentTypeDTO { Name = "Test", Severity = "Minor" };

        _mockRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync((IncidentType?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(code, dto));
    }

    /// <summary>
    /// Test 12: Verifies UpdateAsync throws when setting self as parent
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ThrowsException_WhenSelfReferencing()
    {
        // Arrange
        var code = "T-INC001";
        var existing = new IncidentType { Code = code, Name = "Test", Severity = IncidentSeverity.Minor };
        var dto = new UpdateIncidentTypeDTO
        {
            Name = "Test",
            Severity = "Minor",
            ParentTypeCode = code // Self-reference
        };

        _mockRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync(existing);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(code, dto));
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Test 13: Verifies DeleteAsync deletes incident type successfully
    /// </summary>
    [Fact]
    public async Task DeleteAsync_DeletesIncidentType_WhenNoChildren()
    {
        // Arrange
        var code = "T-INC001";
        var existing = new IncidentType
        {
            Code = code,
            ChildTypes = new List<IncidentType>() // No children
        };

        _mockRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync(existing);
        _mockRepository.Setup(r => r.DeleteAsync(code)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(code);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(code), Times.Once);
    }

    /// <summary>
    /// Test 14: Verifies DeleteAsync returns false when not found
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        var code = "NONEXISTENT";
        _mockRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync((IncidentType?)null);

        // Act
        var result = await _service.DeleteAsync(code);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test 15: Verifies DeleteAsync throws when incident type has children
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenHasChildren()
    {
        // Arrange
        var code = "T-INC001";
        var existing = new IncidentType
        {
            Code = code,
            ChildTypes = new List<IncidentType>
            {
                new IncidentType { Code = "T-INC001-A" }
            }
        };

        _mockRepository.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync(existing);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(code));
    }

    #endregion

    #region DTO Mapping Tests

    /// <summary>
    /// Test 16: Verifies IncidentTypeDTO contains all required fields
    /// </summary>
    [Fact]
    public void IncidentTypeDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new IncidentTypeDTO
        {
            Code = "T-INC001",
            Name = "Equipment Failure",
            Description = "Mechanical failure",
            Severity = "Major",
            ParentTypeCode = null,
            ParentTypeName = null
        };

        // Assert
        Assert.Equal("T-INC001", dto.Code);
        Assert.Equal("Equipment Failure", dto.Name);
        Assert.Equal("Major", dto.Severity);
    }

    /// <summary>
    /// Test 17: Verifies CreateIncidentTypeDTO contains all fields
    /// </summary>
    [Fact]
    public void CreateIncidentTypeDTO_ContainsAllFields()
    {
        // Arrange & Act
        var dto = new CreateIncidentTypeDTO
        {
            Code = "T-INC001",
            Name = "Test",
            Description = "Description",
            Severity = "Minor",
            ParentTypeCode = "T-PARENT"
        };

        // Assert
        Assert.Equal("T-INC001", dto.Code);
        Assert.Equal("T-PARENT", dto.ParentTypeCode);
    }

    /// <summary>
    /// Test 18: Verifies severity enum values
    /// </summary>
    [Theory]
    [InlineData("Minor")]
    [InlineData("Major")]
    [InlineData("Critical")]
    public void IncidentSeverity_HasValidValues(string severityString)
    {
        // Arrange & Act
        var parsed = Enum.TryParse<IncidentSeverity>(severityString, true, out var severity);

        // Assert
        Assert.True(parsed);
    }

    #endregion

    #region Hierarchy Tests

    /// <summary>
    /// Test 19: Verifies incident type can have parent reference
    /// </summary>
    [Fact]
    public void IncidentType_CanHaveParentReference()
    {
        // Arrange & Act
        var parent = new IncidentType { Code = "T-PARENT", Name = "Parent" };
        var child = new IncidentType
        {
            Code = "T-CHILD",
            Name = "Child",
            ParentTypeCode = parent.Code,
            ParentType = parent
        };

        // Assert
        Assert.Equal("T-PARENT", child.ParentTypeCode);
        Assert.NotNull(child.ParentType);
    }

    /// <summary>
    /// Test 20: Verifies incident type can have child types collection
    /// </summary>
    [Fact]
    public void IncidentType_CanHaveChildTypes()
    {
        // Arrange & Act
        var parent = new IncidentType
        {
            Code = "T-PARENT",
            Name = "Parent",
            ChildTypes = new List<IncidentType>
            {
                new IncidentType { Code = "T-CHILD1" },
                new IncidentType { Code = "T-CHILD2" }
            }
        };

        // Assert
        Assert.Equal(2, parent.ChildTypes.Count);
    }

    #endregion
}
