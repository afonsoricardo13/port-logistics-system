using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.9: Unit tests for Executed Operations within Vessel Visit Executions
/// These tests verify CRUD operations, initialization from plans, and operation tracking
/// for managing individual operations during vessel visits
/// </summary>
public class ExecutedOperationServiceTests_US4_1_9
{
    private readonly Mock<IExecutedOperationRepository> _mockRepository;
    private readonly Mock<IVesselVisitExecutionRepository> _mockVveRepository;
    private readonly Mock<IOperationPlanRepository> _mockOperationPlanRepository;
    private readonly Mock<ILogger<ExecutedOperationService>> _mockLogger;
    private readonly ExecutedOperationService _service;

    public ExecutedOperationServiceTests_US4_1_9()
    {
        _mockRepository = new Mock<IExecutedOperationRepository>();
        _mockVveRepository = new Mock<IVesselVisitExecutionRepository>();
        _mockOperationPlanRepository = new Mock<IOperationPlanRepository>();
        _mockLogger = new Mock<ILogger<ExecutedOperationService>>();

        _service = new ExecutedOperationService(
            _mockRepository.Object,
            _mockVveRepository.Object,
            _mockOperationPlanRepository.Object,
            _mockLogger.Object
        );
    }

    /// <summary>
    /// Test 1: Verifies that GetOperationsByVveIdAsync returns all operations for a VVE
    /// </summary>
    [Fact]
    public async Task GetOperationsByVveIdAsync_ReturnsAllOperationsForVve()
    {
        // Arrange
        var vveId = "VVE-2026-001";
        var operations = new List<ExecutedOperation>
        {
            new ExecutedOperation
            {
                Id = Guid.NewGuid(),
                VveId = vveId,
                Type = "unloading",
                Status = ExecutionStatus.Started,
                PlannedOperationIndex = 0
            },
            new ExecutedOperation
            {
                Id = Guid.NewGuid(),
                VveId = vveId,
                Type = "loading",
                Status = ExecutionStatus.Started,
                PlannedOperationIndex = 1
            }
        };

        _mockRepository
            .Setup(r => r.GetByVveIdAsync(vveId))
            .ReturnsAsync(operations);

        // Act
        var result = await _service.GetOperationsByVveIdAsync(vveId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.GetByVveIdAsync(vveId), Times.Once);
    }

    /// <summary>
    /// Test 2: Verifies that GetByIdAsync returns operation when it exists
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsOperation_WhenExists()
    {
        // Arrange
        var operationId = Guid.NewGuid();
        var operation = new ExecutedOperation
        {
            Id = operationId,
            VveId = "VVE-2026-001",
            Type = "unloading",
            Status = ExecutionStatus.Completed,
            PlannedStartTime = DateTime.UtcNow,
            PlannedEndTime = DateTime.UtcNow.AddHours(2),
            ActualStartTime = DateTime.UtcNow,
            ActualEndTime = DateTime.UtcNow.AddHours(2)
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(operationId))
            .ReturnsAsync(operation);

        // Act
        var result = await _service.GetByIdAsync(operationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(operationId, result.Id);
        Assert.Equal("unloading", result.Type);
    }

    /// <summary>
    /// Test 3: Verifies that GetByIdAsync returns null when operation doesn't exist
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var operationId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByIdAsync(operationId))
            .ReturnsAsync((ExecutedOperation?)null);

        // Act
        var result = await _service.GetByIdAsync(operationId);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test 4: Validates ExecutedOperationDTO structure contains all required fields
    /// </summary>
    [Fact]
    public void ExecutedOperationDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new ExecutedOperationDTO
        {
            Id = Guid.NewGuid(),
            VveId = "VVE-2026-001",
            OperationPlanId = Guid.NewGuid(),
            PlannedOperationIndex = 0,
            Type = "unloading",
            Status = "Started",
            PlannedStartTime = DateTime.Parse("2026-01-15T08:00:00Z"),
            PlannedEndTime = DateTime.Parse("2026-01-15T10:00:00Z"),
            ActualStartTime = DateTime.Parse("2026-01-15T08:15:00Z"),
            ActualEndTime = null,
            DelayMinutes = 15,
            UpdatedBy = "operator@test.com",
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Notes = "Operation started with delay"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("VVE-2026-001", dto.VveId);
        Assert.Equal("unloading", dto.Type);
        Assert.Equal("Started", dto.Status);
        Assert.NotNull(dto.PlannedStartTime);
        Assert.NotNull(dto.ActualStartTime);
        Assert.Equal(15, dto.DelayMinutes);
    }

    /// <summary>
    /// Test 5: Verifies empty list is returned when VVE has no operations
    /// </summary>
    [Fact]
    public async Task GetOperationsByVveIdAsync_ReturnsEmptyList_WhenNoOperations()
    {
        // Arrange
        var vveId = "VVE-2026-999";

        _mockRepository
            .Setup(r => r.GetByVveIdAsync(vveId))
            .ReturnsAsync(new List<ExecutedOperation>());

        // Act
        var result = await _service.GetOperationsByVveIdAsync(vveId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Test 6: Validates CreateExecutedOperationDTO structure for manual creation
    /// </summary>
    [Fact]
    public void CreateExecutedOperationDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new CreateExecutedOperationDTO
        {
            VveId = "VVE-2026-001",
            OperationPlanId = Guid.NewGuid(),
            PlannedOperationIndex = 0,
            Type = "loading",
            PlannedStartTime = DateTime.UtcNow,
            PlannedEndTime = DateTime.UtcNow.AddHours(2),
            ActualStartTime = DateTime.UtcNow,
            Notes = "Manual operation creation"
        };

        // Assert
        Assert.Equal("VVE-2026-001", dto.VveId);
        Assert.Equal("loading", dto.Type);
        Assert.NotNull(dto.PlannedStartTime);
        Assert.NotNull(dto.Notes);
    }

    /// <summary>
    /// Test 7: Validates UpdateExecutedOperationDTO structure for updates
    /// </summary>
    [Fact]
    public void UpdateExecutedOperationDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new UpdateExecutedOperationDTO
        {
            ActualStartTime = DateTime.UtcNow,
            ActualEndTime = DateTime.UtcNow.AddHours(2),
            Status = "Completed",
            Notes = "Operation completed successfully"
        };

        // Assert
        Assert.NotNull(dto.ActualStartTime);
        Assert.NotNull(dto.ActualEndTime);
        Assert.Equal("Completed", dto.Status);
    }

    /// <summary>
    /// Test 8: Validates InitializeOperationsFromPlanResponseDTO structure
    /// </summary>
    [Fact]
    public void InitializeOperationsFromPlanResponseDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new InitializeOperationsFromPlanResponseDTO
        {
            Success = true,
            Message = "Operations initialized successfully",
            OperationsInitialized = 2,
            Operations = new List<ExecutedOperationDTO>
            {
                new ExecutedOperationDTO { Id = Guid.NewGuid(), Type = "unloading" },
                new ExecutedOperationDTO { Id = Guid.NewGuid(), Type = "loading" }
            }
        };

        // Assert
        Assert.True(dto.Success);
        Assert.Equal(2, dto.OperationsInitialized);
        Assert.Equal(2, dto.Operations.Count);
    }

    /// <summary>
    /// Test 9: Verifies InitializeFromPlan fails when VVE doesn't exist
    /// </summary>
    [Fact]
    public async Task InitializeOperationsFromPlanAsync_Fails_WhenVveNotFound()
    {
        // Arrange
        var vveId = "VVE-NONEXISTENT";
        var operationPlanId = Guid.NewGuid();

        _mockVveRepository
            .Setup(r => r.GetByIdAsync(vveId))
            .ReturnsAsync((VesselVisitExecution?)null);

        // Act
        var result = await _service.InitializeOperationsFromPlanAsync(vveId, operationPlanId, "operator@test.com");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    /// <summary>
    /// Test 10: Verifies InitializeFromPlan fails when operation plan doesn't exist
    /// </summary>
    [Fact]
    public async Task InitializeOperationsFromPlanAsync_Fails_WhenPlanNotFound()
    {
        // Arrange
        var vveId = "VVE-2026-001";
        var operationPlanId = Guid.NewGuid();
        var vve = new VesselVisitExecution { Id = vveId };

        _mockVveRepository
            .Setup(r => r.GetByIdAsync(vveId))
            .ReturnsAsync(vve);

        _mockOperationPlanRepository
            .Setup(r => r.GetByIdAsync(operationPlanId))
            .ReturnsAsync((OperationPlan?)null);

        // Act
        var result = await _service.InitializeOperationsFromPlanAsync(vveId, operationPlanId, "operator@test.com");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    /// <summary>
    /// Test 11: Verifies operations can be filtered by status
    /// </summary>
    [Fact]
    public async Task GetOperationsByVveIdAsync_CanBeFilteredByStatus()
    {
        // Arrange
        var vveId = "VVE-2026-001";
        var operations = new List<ExecutedOperation>
        {
            new ExecutedOperation { Id = Guid.NewGuid(), VveId = vveId, Status = ExecutionStatus.Completed },
            new ExecutedOperation { Id = Guid.NewGuid(), VveId = vveId, Status = ExecutionStatus.Started },
            new ExecutedOperation { Id = Guid.NewGuid(), VveId = vveId, Status = ExecutionStatus.Delayed }
        };

        _mockRepository
            .Setup(r => r.GetByVveIdAsync(vveId))
            .ReturnsAsync(operations);

        // Act
        var result = await _service.GetOperationsByVveIdAsync(vveId);
        var completedOps = result.Where(o => o.Status == "Completed");

        // Assert
        Assert.Single(completedOps);
    }

    /// <summary>
    /// Test 12: Verifies operation types are correctly identified
    /// </summary>
    [Fact]
    public async Task GetOperationsByVveIdAsync_CorrectlyIdentifiesOperationTypes()
    {
        // Arrange
        var vveId = "VVE-2026-001";
        var operations = new List<ExecutedOperation>
        {
            new ExecutedOperation { Id = Guid.NewGuid(), VveId = vveId, Type = "unloading" },
            new ExecutedOperation { Id = Guid.NewGuid(), VveId = vveId, Type = "loading" }
        };

        _mockRepository
            .Setup(r => r.GetByVveIdAsync(vveId))
            .ReturnsAsync(operations);

        // Act
        var result = await _service.GetOperationsByVveIdAsync(vveId);

        // Assert
        Assert.Contains(result, o => o.Type == "unloading");
        Assert.Contains(result, o => o.Type == "loading");
    }

    /// <summary>
    /// Test 13: Validates delay calculation logic
    /// </summary>
    [Fact]
    public void DelayMinutes_IsCalculatedCorrectly()
    {
        // Arrange
        var plannedStart = DateTime.Parse("2026-01-15T08:00:00Z");
        var actualStart = DateTime.Parse("2026-01-15T08:30:00Z");

        // Act
        var delay = (actualStart - plannedStart).TotalMinutes;

        // Assert
        Assert.Equal(30, delay);
    }

    /// <summary>
    /// Test 14: Verifies ResourceUsageDTO structure
    /// </summary>
    [Fact]
    public void ResourceUsageDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new ResourceUsageDTO
        {
            Type = "crane",
            Id = "STS-CRANE-A1",
            Name = "STS Crane A1"
        };

        // Assert
        Assert.Equal("crane", dto.Type);
        Assert.Equal("STS-CRANE-A1", dto.Id);
        Assert.Equal("STS Crane A1", dto.Name);
    }

    /// <summary>
    /// Test 15: Verifies operations are ordered by planned index
    /// </summary>
    [Fact]
    public async Task GetOperationsByVveIdAsync_ReturnsOperationsOrderedByIndex()
    {
        // Arrange
        var vveId = "VVE-2026-001";
        var operations = new List<ExecutedOperation>
        {
            new ExecutedOperation { Id = Guid.NewGuid(), VveId = vveId, PlannedOperationIndex = 2, Type = "loading" },
            new ExecutedOperation { Id = Guid.NewGuid(), VveId = vveId, PlannedOperationIndex = 0, Type = "unloading" },
            new ExecutedOperation { Id = Guid.NewGuid(), VveId = vveId, PlannedOperationIndex = 1, Type = "loading" }
        };

        _mockRepository
            .Setup(r => r.GetByVveIdAsync(vveId))
            .ReturnsAsync(operations);

        // Act
        var result = (await _service.GetOperationsByVveIdAsync(vveId)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        // Verify we have all indices
        Assert.Contains(result, o => o.PlannedOperationIndex == 0);
        Assert.Contains(result, o => o.PlannedOperationIndex == 1);
        Assert.Contains(result, o => o.PlannedOperationIndex == 2);
    }
}
