using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.11: Unit tests for VesselVisitExecutionService.CompleteAsync
/// Tests cover VVE completion validation, timestamp validation, and business logic
/// </summary>
public class VesselVisitExecutionServiceTests_US4_1_11
{
    private readonly Mock<IVesselVisitExecutionRepository> _mockRepository;
    private readonly Mock<ILogger<VesselVisitExecutionService>> _mockLogger;
    private readonly Mock<PortLogisticsClient> _mockPortLogisticsClient;
    private readonly VesselVisitExecutionService _service;

    public VesselVisitExecutionServiceTests_US4_1_11()
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
    /// Test 1: CompleteAsync should return null when VVE not found
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldReturnNull_WhenVveNotFound()
    {
        // Arrange
        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow,
            ActualDepartureTime = DateTime.UtcNow.AddHours(1)
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-999"))
            .ReturnsAsync((VesselVisitExecution?)null);

        // Act
        var result = await _service.CompleteAsync("VVE-999", dto, "user@example.com");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test 2: CompleteAsync should throw exception when VVE already completed
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenVveAlreadyCompleted()
    {
        // Arrange
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.Completed,
            ActualArrivalTime = DateTime.UtcNow.AddHours(-10),
            ActualBerthTime = DateTime.UtcNow.AddHours(-9)
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow,
            ActualDepartureTime = DateTime.UtcNow.AddHours(1)
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CompleteAsync("VVE-001", dto, "user@example.com")
        );
        Assert.Contains("already completed", exception.Message);
    }

    /// <summary>
    /// Test 3: CompleteAsync should throw exception when ActualArrivalTime is missing
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenActualArrivalTimeMissing()
    {
        // Arrange
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = null,
            ActualBerthTime = DateTime.UtcNow
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow,
            ActualDepartureTime = DateTime.UtcNow.AddHours(1)
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CompleteAsync("VVE-001", dto, "user@example.com")
        );
        Assert.Contains("Actual arrival time is missing", exception.Message);
    }

    /// <summary>
    /// Test 4: CompleteAsync should throw exception when ActualBerthTime is missing
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenActualBerthTimeMissing()
    {
        // Arrange
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = DateTime.UtcNow.AddHours(-10),
            ActualBerthTime = null
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow,
            ActualDepartureTime = DateTime.UtcNow.AddHours(1)
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CompleteAsync("VVE-001", dto, "user@example.com")
        );
        Assert.Contains("Actual berth time is missing", exception.Message);
    }

    /// <summary>
    /// Test 5: CompleteAsync should throw exception when ActualUnberthTime is null
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenActualUnberthTimeNull()
    {
        // Arrange
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = DateTime.UtcNow.AddHours(-10),
            ActualBerthTime = DateTime.UtcNow.AddHours(-9)
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = null,
            ActualDepartureTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CompleteAsync("VVE-001", dto, "user@example.com")
        );
        Assert.Contains("Actual unberth time is required", exception.Message);
    }

    /// <summary>
    /// Test 6: CompleteAsync should throw exception when ActualDepartureTime is null
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenActualDepartureTimeNull()
    {
        // Arrange
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = DateTime.UtcNow.AddHours(-10),
            ActualBerthTime = DateTime.UtcNow.AddHours(-9)
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow,
            ActualDepartureTime = null
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CompleteAsync("VVE-001", dto, "user@example.com")
        );
        Assert.Contains("Actual departure time is required", exception.Message);
    }

    /// <summary>
    /// Test 7: CompleteAsync should throw exception when UnberthTime before BerthTime
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenUnberthTimeBeforeBerthTime()
    {
        // Arrange
        var berthTime = new DateTime(2026, 1, 15, 12, 0, 0);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = berthTime.AddHours(-1), // Before berth time
            ActualDepartureTime = berthTime
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CompleteAsync("VVE-001", dto, "user@example.com")
        );
        Assert.Contains("Unberth time cannot be before berth time", exception.Message);
    }

    /// <summary>
    /// Test 8: CompleteAsync should throw exception when DepartureTime before UnberthTime
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldThrowException_WhenDepartureTimeBeforeUnberthTime()
    {
        // Arrange
        var berthTime = new DateTime(2026, 1, 15, 12, 0, 0);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var unberthTime = berthTime.AddHours(5);
        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = unberthTime,
            ActualDepartureTime = unberthTime.AddHours(-1) // Before unberth time
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CompleteAsync("VVE-001", dto, "user@example.com")
        );
        Assert.Contains("Departure time cannot be before unberth time", exception.Message);
    }

    /// <summary>
    /// Test 9: CompleteAsync should complete VVE successfully with valid data
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldCompleteVveSuccessfully_WithValidData()
    {
        // Arrange
        var berthTime = new DateTime(2026, 1, 15, 12, 0, 0);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime,
            ExecutedOperations = "[{\"type\":\"Loading\"}]"
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = berthTime.AddHours(5),
            ActualDepartureTime = berthTime.AddHours(6),
            Notes = "Completion successful"
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("VVE-001", result.Id);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(dto.ActualUnberthTime, result.ActualUnberthTime);
        Assert.Equal(dto.ActualDepartureTime, result.ActualDepartureTime);
    }

    /// <summary>
    /// Test 10: CompleteAsync should set Status to Completed
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldSetStatus_ToCompleted()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        Assert.Equal("Completed", result!.Status);
    }

    /// <summary>
    /// Test 11: CompleteAsync should set UpdatedBy from parameter
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldSetUpdatedBy_FromParameter()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow
        };

        var completedBy = "admin@example.com";
        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, completedBy);

        // Assert
        Assert.Equal(completedBy, result!.UpdatedBy);
    }

    /// <summary>
    /// Test 12: CompleteAsync should set UpdatedAt to current time
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldSetUpdatedAt_ToCurrentTime()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow
        };

        var beforeComplete = DateTime.UtcNow;
        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, "user@example.com");
        var afterComplete = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result!.UpdatedAt);
        Assert.True(result.UpdatedAt >= beforeComplete);
        Assert.True(result.UpdatedAt <= afterComplete);
    }

    /// <summary>
    /// Test 13: CompleteAsync should append Notes if provided
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldAppendNotes_IfProvided()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime,
            Notes = "Initial notes"
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow,
            Notes = "Completion notes"
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        Assert.Contains("Initial notes", result!.Notes);
        Assert.Contains("Completion notes", result.Notes);
    }

    /// <summary>
    /// Test 14: CompleteAsync should set Notes if VVE has no existing Notes
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldSetNotes_WhenNoExistingNotes()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime,
            Notes = null
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow,
            Notes = "Completion notes"
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        Assert.Equal("Completion notes", result!.Notes);
    }

    /// <summary>
    /// Test 15: CompleteAsync should call UpdateAsync with correct entity
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldCallUpdateAsync_WithCorrectEntity()
    {
        // Arrange
        var berthTime = new DateTime(2026, 1, 15, 12, 0, 0);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = berthTime.AddHours(5),
            ActualDepartureTime = berthTime.AddHours(6)
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<VesselVisitExecution>(v =>
            v.Id == "VVE-001" &&
            v.Status == VesselVisitExecutionStatus.Completed &&
            v.ActualUnberthTime == dto.ActualUnberthTime &&
            v.ActualDepartureTime == dto.ActualDepartureTime &&
            v.UpdatedBy == "user@example.com"
        )), Times.Once);
    }

    /// <summary>
    /// Test 16: CompleteAsync should log information when completing VVE
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldLogInformation_WhenCompletingVve()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("US 4.1.11")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Test 17: CompleteAsync should log warning when ExecutedOperations is empty
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldLogWarning_WhenExecutedOperationsEmpty()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime,
            ExecutedOperations = null
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("without recorded operations")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Test 18: CompleteAsync should accept same time for UnberthTime and DepartureTime
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldAcceptSameTime_ForUnberthAndDeparture()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var sameTime = DateTime.UtcNow;
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = sameTime,
            ActualDepartureTime = sameTime
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sameTime, result.ActualUnberthTime);
        Assert.Equal(sameTime, result.ActualDepartureTime);
    }

    /// <summary>
    /// Test 19: CompleteAsync should update AuditLog
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldUpdateAuditLog()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        Assert.NotNull(result!.AuditLog);
    }

    /// <summary>
    /// Test 20: CompleteAsync should handle null Notes in DTO
    /// </summary>
    [Fact]
    public async Task CompleteAsync_ShouldHandleNullNotes_InDTO()
    {
        // Arrange
        var berthTime = DateTime.UtcNow.AddHours(-5);
        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            Status = VesselVisitExecutionStatus.InProgress,
            ActualArrivalTime = berthTime.AddHours(-1),
            ActualBerthTime = berthTime,
            Notes = "Existing notes"
        };

        var dto = new CompleteVveDTO
        {
            ActualUnberthTime = DateTime.UtcNow.AddHours(-1),
            ActualDepartureTime = DateTime.UtcNow,
            Notes = null
        };

        _mockRepository.Setup(r => r.GetByIdAsync("VVE-001")).ReturnsAsync(vve);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<VesselVisitExecution>()))
            .ReturnsAsync(vve);

        // Act
        var result = await _service.CompleteAsync("VVE-001", dto, "user@example.com");

        // Assert
        Assert.Equal("Existing notes", result!.Notes);
    }
}
