using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.7: Unit tests for VesselVisitExecutionService.CreateAsync
/// Tests cover VVE creation validation and business logic
/// </summary>
public class VesselVisitExecutionServiceTests_US4_1_7
{
    private readonly Mock<IVesselVisitExecutionRepository> _mockRepository;
    private readonly Mock<ILogger<VesselVisitExecutionService>> _mockLogger;
    private readonly Mock<PortLogisticsClient> _mockPortLogisticsClient;
    private readonly VesselVisitExecutionService _service;

    public VesselVisitExecutionServiceTests_US4_1_7()
    {
        _mockRepository = new Mock<IVesselVisitExecutionRepository>();
        _mockLogger = new Mock<ILogger<VesselVisitExecutionService>>();
        _mockPortLogisticsClient = new Mock<PortLogisticsClient>(null!, null!);

        _service = new VesselVisitExecutionService(
            _mockRepository.Object,
            _mockLogger.Object,
            _mockPortLogisticsClient.Object
        );
    }

    /// <summary>
    /// Test 1: CreateAsync should throw exception when VvnId is null
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenVvnIdIsNull()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = null!,
            ActualArrivalTime = DateTime.UtcNow
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(dto, "user@example.com")
        );
        Assert.Contains("VVN ID is required", exception.Message);
    }

    /// <summary>
    /// Test 2: CreateAsync should throw exception when VvnId is empty
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenVvnIdIsEmpty()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "",
            ActualArrivalTime = DateTime.UtcNow
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(dto, "user@example.com")
        );
        Assert.Contains("VVN ID is required", exception.Message);
    }

    /// <summary>
    /// Test 3: CreateAsync should throw exception when ActualArrivalTime is null
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenActualArrivalTimeIsNull()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = null
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(dto, "user@example.com")
        );
        Assert.Contains("Actual arrival time is required", exception.Message);
    }

    /// <summary>
    /// Test 4: CreateAsync should create VVE successfully with valid data
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldCreateVveSuccessfully_WithValidData()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow,
            Notes = "Test notes"
        };

        var expectedId = "VVE-001";
        _mockRepository.Setup(r => r.GenerateNextIdAsync())
            .ReturnsAsync(expectedId);

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedId, result.Id);
        Assert.Equal(dto.VvnId, result.VvnId);
        Assert.Equal(dto.ActualArrivalTime, result.ActualArrivalTime);
        Assert.Equal(dto.Notes, result.Notes);
        Assert.Equal("user@example.com", result.CreatedBy);
        Assert.Equal("InProgress", result.Status);
    }

    /// <summary>
    /// Test 5: CreateAsync should set CreatedBy from parameter
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldSetCreatedBy_FromParameter()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow
        };

        var createdByEmail = "admin@example.com";
        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, createdByEmail);

        // Assert
        Assert.Equal(createdByEmail, result.CreatedBy);
    }

    /// <summary>
    /// Test 6: CreateAsync should set Status to InProgress
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldSetStatus_ToInProgress()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.Equal("InProgress", result.Status);
    }

    /// <summary>
    /// Test 7: CreateAsync should set CreatedAt to current time
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldSetCreatedAt_ToCurrentTime()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow
        };

        var beforeCreate = DateTime.UtcNow;
        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");
        var afterCreate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result.CreatedAt);
        Assert.True(result.CreatedAt >= beforeCreate);
        Assert.True(result.CreatedAt <= afterCreate);
    }

    /// <summary>
    /// Test 8: CreateAsync should call GenerateNextIdAsync
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldCallGenerateNextIdAsync()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        await _service.CreateAsync(dto, "user@example.com");

        // Assert
        _mockRepository.Verify(r => r.GenerateNextIdAsync(), Times.Once);
    }

    /// <summary>
    /// Test 9: CreateAsync should call AddAsync with correct entity
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldCallAddAsync_WithCorrectEntity()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = new DateTime(2026, 1, 15, 10, 30, 0),
            Notes = "Test notes"
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        await _service.CreateAsync(dto, "user@example.com");

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.Is<VesselVisitExecution>(vve =>
            vve.Id == "VVE-001" &&
            vve.VvnId == dto.VvnId &&
            vve.ActualArrivalTime == dto.ActualArrivalTime &&
            vve.Notes == dto.Notes &&
            vve.CreatedBy == "user@example.com" &&
            vve.Status == VesselVisitExecutionStatus.InProgress
        )), Times.Once);
    }

    /// <summary>
    /// Test 10: CreateAsync should handle null Notes
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldHandleNullNotes()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow,
            Notes = null
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.Null(result.Notes);
    }

    /// <summary>
    /// Test 11: CreateAsync should initialize AuditLog
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldInitializeAuditLog()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.NotNull(result.AuditLog);
    }

    /// <summary>
    /// Test 12: CreateAsync should accept VvnId with different formats
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptVvnId_WithDifferentFormats()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN_2026_001",
            ActualArrivalTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.Equal("VVN_2026_001", result.VvnId);
    }

    /// <summary>
    /// Test 13: CreateAsync should accept future ActualArrivalTime
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptFutureActualArrivalTime()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddDays(7);
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = futureTime
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.Equal(futureTime, result.ActualArrivalTime);
    }

    /// <summary>
    /// Test 14: CreateAsync should accept past ActualArrivalTime
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptPastActualArrivalTime()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddDays(-7);
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = pastTime
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.Equal(pastTime, result.ActualArrivalTime);
    }

    /// <summary>
    /// Test 15: CreateAsync should handle long Notes text
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldHandleLongNotesText()
    {
        // Arrange
        var longNotes = new string('A', 1000);
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow,
            Notes = longNotes
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.Equal(longNotes, result.Notes);
        Assert.Equal(1000, result.Notes.Length);
    }

    /// <summary>
    /// Test 16: CreateAsync should log information when creating VVE
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldLogInformation_WhenCreatingVve()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        await _service.CreateAsync(dto, "user@example.com");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("US 4.1.7")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Test 17: CreateAsync should return DTO with all properties set
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldReturnDTO_WithAllPropertiesSet()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = new DateTime(2026, 1, 15, 10, 30, 0),
            Notes = "Test notes"
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.NotNull(result.VvnId);
        Assert.NotNull(result.ActualArrivalTime);
        Assert.NotNull(result.Status);
        Assert.NotNull(result.CreatedBy);
        Assert.NotNull(result.CreatedAt);
        Assert.NotNull(result.Notes);
    }

    /// <summary>
    /// Test 18: CreateAsync should handle whitespace in VvnId
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldAcceptWhitespace_InVvnId()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "  VVN-2026-001  ",
            ActualArrivalTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.Equal("  VVN-2026-001  ", result.VvnId);
    }

    /// <summary>
    /// Test 19: CreateAsync should handle special characters in Notes
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldHandleSpecialCharacters_InNotes()
    {
        // Arrange
        var specialNotes = "Notes with special chars: @#$%^&*()[]{}|\\;:'\",<.>/?";
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow,
            Notes = specialNotes
        };

        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.Equal(specialNotes, result.Notes);
    }

    /// <summary>
    /// Test 20: CreateAsync should return same data that was saved
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldReturnSameData_ThatWasSaved()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = new DateTime(2026, 1, 15, 10, 30, 0),
            Notes = "Test notes"
        };

        VesselVisitExecution savedEntity = null!;
        _mockRepository.Setup(r => r.GenerateNextIdAsync()).ReturnsAsync("VVE-001");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<VesselVisitExecution>()))
            .Callback<VesselVisitExecution>(vve => savedEntity = vve)
            .ReturnsAsync((VesselVisitExecution vve) => vve);

        // Act
        var result = await _service.CreateAsync(dto, "user@example.com");

        // Assert
        Assert.NotNull(savedEntity);
        Assert.Equal(savedEntity.Id, result.Id);
        Assert.Equal(savedEntity.VvnId, result.VvnId);
        Assert.Equal(savedEntity.ActualArrivalTime, result.ActualArrivalTime);
        Assert.Equal(savedEntity.Notes, result.Notes);
        Assert.Equal(savedEntity.CreatedBy, result.CreatedBy);
    }
}
