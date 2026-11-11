using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.13: Unit tests for IncidentService.CreateAsync
/// Tests cover incident creation validation and business logic
/// </summary>
public class IncidentServiceTests_US4_1_13
{
    private readonly Mock<IIncidentRepository> _mockIncidentRepository;
    private readonly Mock<IIncidentTypeRepository> _mockIncidentTypeRepository;
    private readonly Mock<IVesselVisitExecutionRepository> _mockVveRepository;
    private readonly IncidentService _service;

    public IncidentServiceTests_US4_1_13()
    {
        _mockIncidentRepository = new Mock<IIncidentRepository>();
        _mockIncidentTypeRepository = new Mock<IIncidentTypeRepository>();
        _mockVveRepository = new Mock<IVesselVisitExecutionRepository>();

        _service = new IncidentService(
            _mockIncidentRepository.Object,
            _mockIncidentTypeRepository.Object,
            _mockVveRepository.Object
        );
    }

    /// <summary>
    /// Test 1: CreateAsync should throw exception when incident type does not exist
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenIncidentTypeDoesNotExist()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC999",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test incident",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
        Assert.Contains("Incident type with code T-INC999 not found", exception.Message);
    }

    /// <summary>
    /// Test 2: CreateAsync should throw exception when severity is invalid
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenSeverityIsInvalid()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Invalid",
            Description = "Test incident",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(dto)
        );
        Assert.Contains("Invalid severity", exception.Message);
    }

    /// <summary>
    /// Test 3: CreateAsync should create incident successfully with valid data
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldCreateIncidentSuccessfully_WithValidData()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Equipment failure",
            CreatedBy = "user@example.com",
            AffectedVVEIds = new List<string> { "VVE-001" }
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockVveRepository.Setup(r => r.ExistsAsync("VVE-001"))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.IncidentTypeCode, result.IncidentTypeCode);
        Assert.Equal(dto.Severity, result.Severity);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.CreatedBy, result.CreatedBy);
        Assert.Equal("Active", result.Status);
    }

    /// <summary>
    /// Test 4: CreateAsync should set status to Active
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldSetStatus_ToActive()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Minor",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Active", result.Status);
        Assert.True(result.IsActive);
    }

    /// <summary>
    /// Test 5: CreateAsync should set CreatedAt to current time
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldSetCreatedAt_ToCurrentTime()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        var beforeCreate = DateTime.UtcNow;
        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);
        var afterCreate = DateTime.UtcNow;

        // Assert
        Assert.True(result.CreatedAt >= beforeCreate);
        Assert.True(result.CreatedAt <= afterCreate);
    }

    /// <summary>
    /// Test 6: CreateAsync should accept Minor severity
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptMinorSeverity()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Minor",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Minor", result.Severity);
    }

    /// <summary>
    /// Test 7: CreateAsync should accept Major severity
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptMajorSeverity()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Major", result.Severity);
    }

    /// <summary>
    /// Test 8: CreateAsync should accept Critical severity
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptCriticalSeverity()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Critical",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Critical", result.Severity);
    }

    /// <summary>
    /// Test 9: CreateAsync should use wildcard when no VVEs specified
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldUseWildcard_WhenNoVVEsSpecified()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com",
            AffectedVVEIds = null
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result.AffectedVVEIds);
        Assert.Single(result.AffectedVVEIds);
        Assert.Equal("*", result.AffectedVVEIds[0]);
    }

    /// <summary>
    /// Test 10: CreateAsync should validate VVE existence
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldValidateVVEExistence()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com",
            AffectedVVEIds = new List<string> { "VVE-999" }
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockVveRepository.Setup(r => r.ExistsAsync("VVE-999"))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(dto)
        );
        Assert.Contains("Vessel visit execution with ID VVE-999 not found", exception.Message);
    }

    /// <summary>
    /// Test 11: CreateAsync should accept multiple affected VVEs
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptMultipleAffectedVVEs()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com",
            AffectedVVEIds = new List<string> { "VVE-001", "VVE-002", "VVE-003" }
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockVveRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(3, result.AffectedVVEIds.Count);
        Assert.Contains("VVE-001", result.AffectedVVEIds);
        Assert.Contains("VVE-002", result.AffectedVVEIds);
        Assert.Contains("VVE-003", result.AffectedVVEIds);
    }

    /// <summary>
    /// Test 12: CreateAsync should call AddAsync on repository
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldCallAddAsync()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _mockIncidentRepository.Verify(r => r.AddAsync(It.Is<Incident>(i =>
            i.IncidentTypeCode == dto.IncidentTypeCode &&
            i.Severity == IncidentSeverity.Major &&
            i.Status == IncidentStatus.Active
        )), Times.Once);
    }

    /// <summary>
    /// Test 13: CreateAsync should accept past StartTime
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptPastStartTime()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddHours(-5);
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = pastTime,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(pastTime, result.StartTime);
    }

    /// <summary>
    /// Test 14: CreateAsync should accept future StartTime
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptFutureStartTime()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddHours(5);
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = futureTime,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(futureTime, result.StartTime);
    }

    /// <summary>
    /// Test 15: CreateAsync should handle long descriptions
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldHandleLongDescriptions()
    {
        // Arrange
        var longDescription = new string('A', 1000);
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = longDescription,
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(longDescription, result.Description);
        Assert.Equal(1000, result.Description.Length);
    }

    /// <summary>
    /// Test 16: CreateAsync should generate new Guid for incident
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldGenerateNewGuidForIncident()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    /// <summary>
    /// Test 17: CreateAsync should handle empty affected VVEs list
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldHandleEmptyAffectedVVEsList()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com",
            AffectedVVEIds = new List<string>()
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Single(result.AffectedVVEIds);
        Assert.Equal("*", result.AffectedVVEIds[0]);
    }

    /// <summary>
    /// Test 18: CreateAsync should accept wildcard in affected VVEs
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptWildcardInAffectedVVEs()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com",
            AffectedVVEIds = new List<string> { "*" }
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Single(result.AffectedVVEIds);
        Assert.Equal("*", result.AffectedVVEIds[0]);
    }

    /// <summary>
    /// Test 19: CreateAsync should handle special characters in description
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldHandleSpecialCharactersInDescription()
    {
        // Arrange
        var specialDescription = "Incident: @#$%^&*()[]{}|\\;:'\",<.>/?";
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = specialDescription,
            CreatedBy = "user@example.com"
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(specialDescription, result.Description);
    }

    /// <summary>
    /// Test 20: CreateAsync should return incident with all properties set
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldReturnIncidentWithAllPropertiesSet()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test incident",
            CreatedBy = "user@example.com",
            AffectedVVEIds = new List<string> { "VVE-001" }
        };

        _mockIncidentTypeRepository.Setup(r => r.ExistsAsync(dto.IncidentTypeCode))
            .ReturnsAsync(true);
        _mockVveRepository.Setup(r => r.ExistsAsync("VVE-001"))
            .ReturnsAsync(true);
        _mockIncidentRepository.Setup(r => r.AddAsync(It.IsAny<Incident>()))
            .ReturnsAsync((Incident i) => i);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(dto.IncidentTypeCode, result.IncidentTypeCode);
        Assert.Equal(dto.StartTime, result.StartTime);
        Assert.Equal(dto.Severity, result.Severity);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(dto.CreatedBy, result.CreatedBy);
        Assert.Equal("Active", result.Status);
        Assert.NotNull(result.AffectedVVEIds);
        Assert.Contains("VVE-001", result.AffectedVVEIds);
    }
}
