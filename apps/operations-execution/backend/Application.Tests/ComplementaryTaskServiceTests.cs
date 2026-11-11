using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

public class ComplementaryTaskServiceTests
{
    private readonly Mock<IComplementaryTaskRepository> _taskRepoMock;
    private readonly Mock<IComplementaryTaskCategoryRepository> _categoryRepoMock;
    private readonly Mock<IVesselVisitExecutionRepository> _vveRepoMock;
    private readonly ComplementaryTaskService _service;

    public ComplementaryTaskServiceTests()
    {
        _taskRepoMock = new Mock<IComplementaryTaskRepository>();
        _categoryRepoMock = new Mock<IComplementaryTaskCategoryRepository>();
        _vveRepoMock = new Mock<IVesselVisitExecutionRepository>();
        _service = new ComplementaryTaskService(_taskRepoMock.Object, _categoryRepoMock.Object, _vveRepoMock.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithoutFilter_ReturnsAllTasks()
    {
        // Arrange
        var tasks = new List<ComplementaryTask>
        {
            CreateTask("T1", ComplementaryTaskStatus.Ongoing),
            CreateTask("T2", ComplementaryTaskStatus.Completed)
        };
        _taskRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithOngoingFilter_ReturnsOnlyOngoingTasks()
    {
        // Arrange
        var ongoingTasks = new List<ComplementaryTask>
        {
            CreateTask("T1", ComplementaryTaskStatus.Ongoing)
        };
        _taskRepoMock.Setup(r => r.GetOngoingTasksAsync()).ReturnsAsync(ongoingTasks);

        var filter = new ComplementaryTaskFilterDTO { OngoingOnly = true };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        Assert.Single(result);
        Assert.All(result, t => Assert.Equal("Ongoing", t.Status));
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsTasksWithStatus()
    {
        // Arrange
        var completedTasks = new List<ComplementaryTask>
        {
            CreateTask("T1", ComplementaryTaskStatus.Completed)
        };
        _taskRepoMock.Setup(r => r.GetByStatusAsync(ComplementaryTaskStatus.Completed))
            .ReturnsAsync(completedTasks);

        var filter = new ComplementaryTaskFilterDTO { Status = "Completed" };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        Assert.Single(result);
        Assert.All(result, t => Assert.Equal("Completed", t.Status));
    }

    [Fact]
    public async Task GetAllAsync_WithVveIdFilter_ReturnsTasksForVve()
    {
        // Arrange
        var vveId = "VVE-001";
        var tasks = new List<ComplementaryTask>
        {
            CreateTask("T1", ComplementaryTaskStatus.Ongoing, vveId)
        };
        _taskRepoMock.Setup(r => r.GetByVveIdAsync(vveId)).ReturnsAsync(tasks);

        var filter = new ComplementaryTaskFilterDTO { VveId = vveId };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        Assert.Single(result);
        Assert.All(result, t => Assert.Equal(vveId, t.VveId));
    }

    [Fact]
    public async Task GetAllAsync_WithDateRangeFilter_ReturnsTasksInRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var tasks = new List<ComplementaryTask>
        {
            CreateTask("T1", ComplementaryTaskStatus.Ongoing)
        };
        _taskRepoMock.Setup(r => r.GetByDateRangeAsync(startDate, endDate)).ReturnsAsync(tasks);

        var filter = new ComplementaryTaskFilterDTO { StartDate = startDate, EndDate = endDate };

        // Act
        var result = await _service.GetAllAsync(filter);

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = CreateTask("T1", ComplementaryTaskStatus.Ongoing);
        task.Id = taskId;
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

        // Act
        var result = await _service.GetByIdAsync(taskId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync((ComplementaryTask?)null);

        // Act
        var result = await _service.GetByIdAsync(taskId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidData_CreatesTask()
    {
        // Arrange
        var dto = new CreateComplementaryTaskDTO
        {
            CategoryCode = "CTC001",
            VveId = "VVE-001",
            ResponsibleTeam = "Maintenance Team",
            StartTime = DateTime.UtcNow,
            ImpactType = "Blocking",
            Notes = "Hull inspection"
        };

        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            Status = VesselVisitExecutionStatus.InProgress
        };

        _categoryRepoMock.Setup(r => r.ExistsAsync(dto.CategoryCode)).ReturnsAsync(true);
        _vveRepoMock.Setup(r => r.GetByIdAsync(dto.VveId)).ReturnsAsync(vve);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<ComplementaryTask>()))
            .ReturnsAsync((ComplementaryTask t) => t);
        _taskRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => 
            {
                var task = new ComplementaryTask
                {
                    Id = id,
                    CategoryCode = dto.CategoryCode,
                    Category = new ComplementaryTaskCategory { Code = dto.CategoryCode, Name = "Inspection" },
                    VveId = dto.VveId,
                    ResponsibleTeam = dto.ResponsibleTeam,
                    StartTime = dto.StartTime,
                    Status = ComplementaryTaskStatus.Ongoing,
                    ImpactType = TaskImpactType.Blocking,
                    CreatedAt = DateTime.UtcNow
                };
                return task;
            });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.CategoryCode, result.CategoryCode);
        Assert.Equal(dto.VveId, result.VveId);
        Assert.Equal(dto.ResponsibleTeam, result.ResponsibleTeam);
        Assert.Equal("Blocking", result.ImpactType);
        Assert.Equal("Ongoing", result.Status);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategory_ThrowsException()
    {
        // Arrange
        var dto = new CreateComplementaryTaskDTO
        {
            CategoryCode = "INVALID",
            VveId = "VVE-001",
            ResponsibleTeam = "Team",
            StartTime = DateTime.UtcNow,
            ImpactType = "Parallel"
        };

        _categoryRepoMock.Setup(r => r.ExistsAsync(dto.CategoryCode)).ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
        Assert.Contains("category", ex.Message.ToLower());
    }

    [Fact]
    public async Task CreateAsync_InvalidVve_ThrowsException()
    {
        // Arrange
        var dto = new CreateComplementaryTaskDTO
        {
            CategoryCode = "CTC001",
            VveId = "INVALID",
            ResponsibleTeam = "Team",
            StartTime = DateTime.UtcNow,
            ImpactType = "Parallel"
        };

        _categoryRepoMock.Setup(r => r.ExistsAsync(dto.CategoryCode)).ReturnsAsync(true);
        _vveRepoMock.Setup(r => r.GetByIdAsync(dto.VveId)).ReturnsAsync((VesselVisitExecution?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
        Assert.Contains("vessel visit execution", ex.Message.ToLower());
    }

    [Fact]
    public async Task CreateAsync_VveNotInProgress_ThrowsException()
    {
        // Arrange
        var dto = new CreateComplementaryTaskDTO
        {
            CategoryCode = "CTC001",
            VveId = "VVE-001",
            ResponsibleTeam = "Team",
            StartTime = DateTime.UtcNow,
            ImpactType = "Parallel"
        };

        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            Status = VesselVisitExecutionStatus.Completed
        };

        _categoryRepoMock.Setup(r => r.ExistsAsync(dto.CategoryCode)).ReturnsAsync(true);
        _vveRepoMock.Setup(r => r.GetByIdAsync(dto.VveId)).ReturnsAsync(vve);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
        Assert.Contains("InProgress", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidImpactType_ThrowsException()
    {
        // Arrange
        var dto = new CreateComplementaryTaskDTO
        {
            CategoryCode = "CTC001",
            VveId = "VVE-001",
            ResponsibleTeam = "Team",
            StartTime = DateTime.UtcNow,
            ImpactType = "INVALID"
        };

        var vve = new VesselVisitExecution
        {
            Id = "VVE-001",
            Status = VesselVisitExecutionStatus.InProgress
        };

        _categoryRepoMock.Setup(r => r.ExistsAsync(dto.CategoryCode)).ReturnsAsync(true);
        _vveRepoMock.Setup(r => r.GetByIdAsync(dto.VveId)).ReturnsAsync(vve);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        Assert.Contains("impact type", ex.Message.ToLower());
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var existingTask = CreateTask("T1", ComplementaryTaskStatus.Ongoing);
        existingTask.Id = taskId;

        var dto = new UpdateComplementaryTaskDTO
        {
            ResponsibleTeam = "New Team",
            Status = "Completed",
            EndTime = DateTime.UtcNow
        };

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existingTask);
        _taskRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ComplementaryTask>()))
            .ReturnsAsync((ComplementaryTask t) => t);

        // Act
        var result = await _service.UpdateAsync(taskId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingTask_ThrowsException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var dto = new UpdateComplementaryTaskDTO();

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync((ComplementaryTask?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(taskId, dto));
        Assert.Contains("not found", ex.Message.ToLower());
    }

    [Fact]
    public async Task UpdateAsync_EndTimeBeforeStartTime_ThrowsException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var existingTask = CreateTask("T1", ComplementaryTaskStatus.Ongoing);
        existingTask.Id = taskId;
        existingTask.StartTime = DateTime.UtcNow;

        var dto = new UpdateComplementaryTaskDTO
        {
            EndTime = DateTime.UtcNow.AddHours(-1)
        };

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existingTask);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(taskId, dto));
        Assert.Contains("after start time", ex.Message.ToLower());
    }

    [Fact]
    public async Task UpdateAsync_CompletedStatus_AutoSetsEndTime()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var existingTask = CreateTask("T1", ComplementaryTaskStatus.Ongoing);
        existingTask.Id = taskId;

        var dto = new UpdateComplementaryTaskDTO
        {
            Status = "Completed"
        };

        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existingTask);
        _taskRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ComplementaryTask>()))
            .ReturnsAsync((ComplementaryTask t) => t);

        // Act
        var result = await _service.UpdateAsync(taskId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        _taskRepoMock.Verify(r => r.UpdateAsync(It.Is<ComplementaryTask>(t => t.EndTime.HasValue)), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingTask_DeletesSuccessfully()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskRepoMock.Setup(r => r.ExistsAsync(taskId)).ReturnsAsync(true);
        _taskRepoMock.Setup(r => r.DeleteAsync(taskId)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(taskId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingTask_ThrowsException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskRepoMock.Setup(r => r.ExistsAsync(taskId)).ReturnsAsync(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(taskId));
        Assert.Contains("not found", ex.Message.ToLower());
    }

    #endregion

    #region Helper Methods

    private ComplementaryTask CreateTask(string id, ComplementaryTaskStatus status, string vveId = "VVE-001")
    {
        return new ComplementaryTask
        {
            Id = Guid.NewGuid(),
            CategoryCode = "CTC001",
            Category = new ComplementaryTaskCategory { Code = "CTC001", Name = "Inspection" },
            VveId = vveId,
            ResponsibleTeam = "Team A",
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = status == ComplementaryTaskStatus.Completed ? DateTime.UtcNow : null,
            Status = status,
            ImpactType = TaskImpactType.Parallel,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };
    }

    #endregion
}
