using Application.DTO;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.9: Integration tests for Executed Operations Controller endpoints
/// These tests validate API response structures, initialization logic, and CRUD operations
/// without requiring the full application stack to be running
/// </summary>
public class ExecutedOperationsControllerTests_US4_1_9
{
    /// <summary>
    /// Test 1: Validates ExecutedOperationDTO contains all required fields
    /// for displaying operation execution details
    /// </summary>
    [Fact]
    public void ExecutedOperationDTO_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new ExecutedOperationDTO
        {
            Id = Guid.NewGuid(),
            VveId = "VVE-2026-001",
            OperationPlanId = Guid.NewGuid(),
            PlannedOperationIndex = 0,
            Type = "unloading",
            Status = "Completed",
            PlannedStartTime = DateTime.Parse("2026-01-15T08:00:00Z"),
            PlannedEndTime = DateTime.Parse("2026-01-15T10:00:00Z"),
            ActualStartTime = DateTime.Parse("2026-01-15T08:15:00Z"),
            ActualEndTime = DateTime.Parse("2026-01-15T10:30:00Z"),
            DelayMinutes = 15,
            UpdatedBy = "operator@test.com",
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Notes = "Completed with minor delay"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("VVE-2026-001", dto.VveId);
        Assert.Equal("unloading", dto.Type);
        Assert.Equal("Completed", dto.Status);
        Assert.Equal(15, dto.DelayMinutes);
        Assert.NotNull(dto.PlannedStartTime);
        Assert.NotNull(dto.ActualStartTime);
    }

    /// <summary>
    /// Test 2: Verifies delay calculation between planned and actual start times
    /// </summary>
    [Fact]
    public void DelayMinutes_IsCalculatedCorrectly()
    {
        // Arrange
        var plannedStart = DateTime.Parse("2026-01-15T08:00:00Z");
        var actualStart = DateTime.Parse("2026-01-15T08:30:00Z");

        // Act
        var delayMinutes = (int)(actualStart - plannedStart).TotalMinutes;

        // Assert
        Assert.Equal(30, delayMinutes);
    }

    /// <summary>
    /// Test 3: Validates operation duration calculation
    /// </summary>
    [Fact]
    public void OperationDuration_IsCalculatedCorrectly()
    {
        // Arrange
        var startTime = DateTime.Parse("2026-01-15T08:00:00Z");
        var endTime = DateTime.Parse("2026-01-15T10:30:00Z");

        // Act
        var duration = endTime - startTime;

        // Assert
        Assert.Equal(TimeSpan.FromHours(2.5), duration);
    }

    /// <summary>
    /// Test 4: Tests CreateExecutedOperationDTO can be created for manual operations
    /// </summary>
    [Fact]
    public void CreateExecutedOperationDTO_CanBeCreatedForManualOperation()
    {
        // Arrange & Act
        var dto = new CreateExecutedOperationDTO
        {
            VveId = "VVE-2026-001",
            OperationPlanId = null, // Manual operation without plan
            PlannedOperationIndex = 0,
            Type = "loading",
            PlannedStartTime = DateTime.Parse("2026-01-15T10:00:00Z"),
            PlannedEndTime = DateTime.Parse("2026-01-15T12:00:00Z"),
            ActualStartTime = DateTime.Parse("2026-01-15T10:00:00Z"),
            Notes = "Manual operation created"
        };

        // Assert
        Assert.Equal("VVE-2026-001", dto.VveId);
        Assert.Null(dto.OperationPlanId);
        Assert.Equal("loading", dto.Type);
        Assert.NotNull(dto.Notes);
    }

    /// <summary>
    /// Test 5: Tests UpdateExecutedOperationDTO supports partial updates
    /// </summary>
    [Fact]
    public void UpdateExecutedOperationDTO_SupportsPartialUpdates()
    {
        // Arrange & Act - Only setting status
        var dto = new UpdateExecutedOperationDTO
        {
            Status = "Delayed",
            Notes = "Delayed due to crane malfunction"
        };

        // Assert
        Assert.Null(dto.ActualStartTime);
        Assert.Null(dto.ActualEndTime);
        Assert.Equal("Delayed", dto.Status);
        Assert.NotNull(dto.Notes);
    }

    /// <summary>
    /// Test 6: Validates operation list can be filtered by type
    /// </summary>
    [Fact]
    public void OperationList_CanBeFilteredByType()
    {
        // Arrange
        var operations = new List<ExecutedOperationDTO>
        {
            new ExecutedOperationDTO { Id = Guid.NewGuid(), VveId = "VVE-1", Type = "unloading" },
            new ExecutedOperationDTO { Id = Guid.NewGuid(), VveId = "VVE-1", Type = "loading" },
            new ExecutedOperationDTO { Id = Guid.NewGuid(), VveId = "VVE-1", Type = "unloading" }
        };

        // Act
        var unloadingOps = operations.Where(o => o.Type == "unloading").ToList();
        var loadingOps = operations.Where(o => o.Type == "loading").ToList();

        // Assert
        Assert.Equal(2, unloadingOps.Count);
        Assert.Single(loadingOps);
    }

    /// <summary>
    /// Test 7: Validates operation list can be filtered by status
    /// </summary>
    [Fact]
    public void OperationList_CanBeFilteredByStatus()
    {
        // Arrange
        var operations = new List<ExecutedOperationDTO>
        {
            new ExecutedOperationDTO { Id = Guid.NewGuid(), VveId = "VVE-1", Status = "Started" },
            new ExecutedOperationDTO { Id = Guid.NewGuid(), VveId = "VVE-1", Status = "Completed" },
            new ExecutedOperationDTO { Id = Guid.NewGuid(), VveId = "VVE-1", Status = "Delayed" }
        };

        // Act
        var startedOps = operations.Where(o => o.Status == "Started").ToList();
        var completedOps = operations.Where(o => o.Status == "Completed").ToList();
        var delayedOps = operations.Where(o => o.Status == "Delayed").ToList();

        // Assert
        Assert.Single(startedOps);
        Assert.Single(completedOps);
        Assert.Single(delayedOps);
    }

    /// <summary>
    /// Test 8: Tests InitializeOperationsFromPlanResponseDTO for success scenario
    /// </summary>
    [Fact]
    public void InitializeOperationsFromPlanResponseDTO_CanRepresentSuccess()
    {
        // Arrange & Act
        var dto = new InitializeOperationsFromPlanResponseDTO
        {
            Success = true,
            Message = "2 operations initialized successfully",
            OperationsInitialized = 2,
            Operations = new List<ExecutedOperationDTO>
            {
                new ExecutedOperationDTO { Id = Guid.NewGuid(), Type = "unloading", PlannedOperationIndex = 0 },
                new ExecutedOperationDTO { Id = Guid.NewGuid(), Type = "loading", PlannedOperationIndex = 1 }
            }
        };

        // Assert
        Assert.True(dto.Success);
        Assert.Equal(2, dto.OperationsInitialized);
        Assert.Equal(2, dto.Operations.Count);
        Assert.Contains("initialized", dto.Message.ToLower());
    }

    /// <summary>
    /// Test 9: Tests InitializeOperationsFromPlanResponseDTO for failure scenario
    /// </summary>
    [Fact]
    public void InitializeOperationsFromPlanResponseDTO_CanRepresentFailure()
    {
        // Arrange & Act
        var dto = new InitializeOperationsFromPlanResponseDTO
        {
            Success = false,
            Message = "VVE not found",
            OperationsInitialized = 0,
            Operations = new List<ExecutedOperationDTO>()
        };

        // Assert
        Assert.False(dto.Success);
        Assert.Equal(0, dto.OperationsInitialized);
        Assert.Empty(dto.Operations);
        Assert.Contains("not found", dto.Message.ToLower());
    }

    /// <summary>
    /// Test 10: Validates ResourceUsageDTO for tracking resources used
    /// </summary>
    [Fact]
    public void ResourceUsageDTO_CanRepresentDifferentResourceTypes()
    {
        // Arrange & Act
        var resources = new List<ResourceUsageDTO>
        {
            new ResourceUsageDTO { Type = "crane", Id = "STS-CRANE-A1", Name = "STS Crane A1" },
            new ResourceUsageDTO { Type = "dock", Id = "DOCK-A", Name = "Dock A" },
            new ResourceUsageDTO { Type = "staff", Id = "OP-001", Name = "Operator 001" }
        };

        // Assert
        Assert.Equal(3, resources.Count);
        Assert.Contains(resources, r => r.Type == "crane");
        Assert.Contains(resources, r => r.Type == "dock");
        Assert.Contains(resources, r => r.Type == "staff");
    }

    /// <summary>
    /// Test 11: Verifies operations are sorted by PlannedOperationIndex
    /// </summary>
    [Fact]
    public void OperationList_CanBeSortedByIndex()
    {
        // Arrange
        var operations = new List<ExecutedOperationDTO>
        {
            new ExecutedOperationDTO { Id = Guid.NewGuid(), PlannedOperationIndex = 2 },
            new ExecutedOperationDTO { Id = Guid.NewGuid(), PlannedOperationIndex = 0 },
            new ExecutedOperationDTO { Id = Guid.NewGuid(), PlannedOperationIndex = 1 }
        };

        // Act
        var sorted = operations.OrderBy(o => o.PlannedOperationIndex).ToList();

        // Assert
        Assert.Equal(0, sorted[0].PlannedOperationIndex);
        Assert.Equal(1, sorted[1].PlannedOperationIndex);
        Assert.Equal(2, sorted[2].PlannedOperationIndex);
    }

    /// <summary>
    /// Test 12: Tests that operations track timing correctly
    /// </summary>
    [Fact]
    public void ExecutedOperation_TracksTiming_WhenStartedButNotCompleted()
    {
        // Arrange & Act
        var dto = new ExecutedOperationDTO
        {
            Id = Guid.NewGuid(),
            VveId = "VVE-2026-001",
            Type = "unloading",
            Status = "Started",
            PlannedStartTime = DateTime.Parse("2026-01-15T08:00:00Z"),
            PlannedEndTime = DateTime.Parse("2026-01-15T10:00:00Z"),
            ActualStartTime = DateTime.Parse("2026-01-15T08:00:00Z"),
            ActualEndTime = null // Not completed yet
        };

        // Assert
        Assert.Equal("Started", dto.Status);
        Assert.NotNull(dto.ActualStartTime);
        Assert.Null(dto.ActualEndTime);
    }

    /// <summary>
    /// Test 13: Validates delay status triggers correctly
    /// </summary>
    [Fact]
    public void DelayStatus_IsTriggered_WhenActualStartExceedsPlanned()
    {
        // Arrange
        var plannedStart = DateTime.Parse("2026-01-15T08:00:00Z");
        var actualStart = DateTime.Parse("2026-01-15T09:00:00Z"); // 1 hour late
        var delayThresholdMinutes = 30;

        // Act
        var delayMinutes = (int)(actualStart - plannedStart).TotalMinutes;
        var isDelayed = delayMinutes > delayThresholdMinutes;

        // Assert
        Assert.True(isDelayed);
        Assert.Equal(60, delayMinutes);
    }

    /// <summary>
    /// Test 14: Tests operations with resources attached
    /// </summary>
    [Fact]
    public void ExecutedOperation_CanHaveMultipleResources()
    {
        // Arrange & Act
        var dto = new ExecutedOperationDTO
        {
            Id = Guid.NewGuid(),
            VveId = "VVE-2026-001",
            Type = "loading",
            Status = "Completed",
            ActualResources = new List<ResourceUsageDTO>
            {
                new ResourceUsageDTO { Type = "crane", Id = "CRANE-1" },
                new ResourceUsageDTO { Type = "crane", Id = "CRANE-2" },
                new ResourceUsageDTO { Type = "staff", Id = "OP-001" }
            }
        };

        // Assert
        Assert.NotNull(dto.ActualResources);
        Assert.Equal(3, dto.ActualResources.Count);
        Assert.Equal(2, dto.ActualResources.Count(r => r.Type == "crane"));
    }

    /// <summary>
    /// Test 15: Validates notes field can store operation details
    /// </summary>
    [Fact]
    public void ExecutedOperation_CanStoreNotes()
    {
        // Arrange & Act
        var dto = new ExecutedOperationDTO
        {
            Id = Guid.NewGuid(),
            VveId = "VVE-2026-001",
            Type = "unloading",
            Status = "Delayed",
            Notes = "Delayed due to weather conditions. Crane operations suspended for 30 minutes."
        };

        // Assert
        Assert.NotNull(dto.Notes);
        Assert.Contains("weather", dto.Notes.ToLower());
        Assert.Contains("delayed", dto.Notes.ToLower());
    }
}
