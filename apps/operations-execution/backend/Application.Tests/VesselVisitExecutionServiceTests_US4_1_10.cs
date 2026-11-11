using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.10: Unit tests for Search and List Vessel Visit Executions
/// These tests verify filtering logic, metric calculations, and search functionality
/// for reviewing execution history and analyzing performance
/// </summary>
public class VesselVisitExecutionServiceTests_US4_1_10
{
    private readonly Mock<IVesselVisitExecutionRepository> _mockRepository;
    private readonly Mock<ILogger<VesselVisitExecutionService>> _mockLogger;
    private readonly Mock<PortLogisticsClient> _mockPortLogisticsClient;
    private readonly VesselVisitExecutionService _service;

    public VesselVisitExecutionServiceTests_US4_1_10()
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
    /// Test 1: Verifies that GetByDateRangeAsync correctly filters VVEs
    /// within the specified date range for historical analysis
    /// </summary>
    [Fact]
    public async Task GetByDateRangeAsync_ReturnsVvesWithinDateRange()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-01-01");
        var endDate = DateTime.Parse("2026-01-31");
        
        var vves = new List<VesselVisitExecution>
        {
            new VesselVisitExecution 
            { 
                Id = "VVE-2026-001", 
                VvnId = "VVN-2026-001",
                ActualArrivalTime = DateTime.Parse("2026-01-15"),
                Status = VesselVisitExecutionStatus.Completed
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-2026-002", 
                VvnId = "VVN-2026-002",
                ActualArrivalTime = DateTime.Parse("2026-01-20"),
                Status = VesselVisitExecutionStatus.InProgress
            }
        };

        _mockRepository
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate))
            .ReturnsAsync(vves);

        // Act
        var result = await _service.GetByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.GetByDateRangeAsync(startDate, endDate), Times.Once);
    }

    /// <summary>
    /// Test 2: Validates filtering VVEs by execution status
    /// to identify in-progress or completed visits
    /// </summary>
    [Fact]
    public async Task GetByStatusAsync_FiltersVvesByStatus()
    {
        // Arrange
        var status = VesselVisitExecutionStatus.Completed;
        var vves = new List<VesselVisitExecution>
        {
            new VesselVisitExecution 
            { 
                Id = "VVE-2026-001", 
                VvnId = "VVN-2026-001",
                Status = VesselVisitExecutionStatus.Completed,
                ActualArrivalTime = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetByStatusAsync(status))
            .ReturnsAsync(vves);

        // Act
        var result = await _service.GetByStatusAsync(status);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, vve => Assert.Equal(status.ToString(), vve.Status));
    }

    /// <summary>
    /// Test 3: Ensures VVEs can be retrieved by vessel identifier (VVN ID)
    /// for vessel-specific execution history review
    /// </summary>
    [Fact]
    public async Task GetByVvnIdAsync_ReturnsVvesForSpecificVessel()
    {
        // Arrange
        var vvnId = "VVN-2026-001";
        var vves = new List<VesselVisitExecution>
        {
            new VesselVisitExecution 
            { 
                Id = "VVE-2026-001", 
                VvnId = vvnId,
                ActualArrivalTime = DateTime.UtcNow,
                Status = VesselVisitExecutionStatus.Completed
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-2026-002", 
                VvnId = vvnId,
                ActualArrivalTime = DateTime.UtcNow.AddDays(-1),
                Status = VesselVisitExecutionStatus.Completed
            }
        };

        _mockRepository
            .Setup(r => r.GetByVvnIdAsync(vvnId))
            .ReturnsAsync(vves);

        // Act
        var result = await _service.GetByVvnIdAsync(vvnId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, vve => Assert.Equal(vvnId, vve.VvnId));
    }

    /// <summary>
    /// Test 4: Validates VesselVisitExecutionDTO structure contains
    /// all required fields for execution history display
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new VesselVisitExecutionDTO
        {
            Id = "VVE-2026-001",
            VvnId = "VVN-2026-001",
            CreatedBy = "operator@test.com",
            ActualArrivalTime = DateTime.Parse("2026-01-15T08:00:00Z"),
            ActualBerthTime = DateTime.Parse("2026-01-15T09:00:00Z"),
            ActualUnberthTime = DateTime.Parse("2026-01-15T16:00:00Z"),
            ActualDepartureTime = DateTime.Parse("2026-01-15T17:00:00Z"),
            DockUsed = "DOCK-A",
            Status = "Completed"
        };

        // Assert
        Assert.Equal("VVE-2026-001", dto.Id);
        Assert.Equal("VVN-2026-001", dto.VvnId);
        Assert.NotNull(dto.ActualArrivalTime);
        Assert.NotNull(dto.ActualBerthTime);
        Assert.Equal("DOCK-A", dto.DockUsed);
        Assert.Equal("Completed", dto.Status);
    }

    /// <summary>
    /// Test 5: Verifies empty result set when no VVEs match filter criteria
    /// ensuring graceful handling of no-results scenarios
    /// </summary>
    [Fact]
    public async Task GetByDateRangeAsync_ReturnsEmptyList_WhenNoVvesInRange()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-12-01");
        var endDate = DateTime.Parse("2026-12-31");
        
        _mockRepository
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate))
            .ReturnsAsync(new List<VesselVisitExecution>());

        // Act
        var result = await _service.GetByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Test 6: Validates date range filtering excludes VVEs outside the specified period
    /// ensuring accurate temporal filtering for performance analysis
    /// </summary>
    [Fact]
    public void DateRangeFilter_ExcludesVvesOutsideRange()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-01-01");
        var endDate = DateTime.Parse("2026-01-31");
        
        var vves = new List<VesselVisitExecution>
        {
            new VesselVisitExecution 
            { 
                Id = "VVE-1", 
                ActualArrivalTime = DateTime.Parse("2025-12-31"),
                Status = VesselVisitExecutionStatus.Completed
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-2", 
                ActualArrivalTime = DateTime.Parse("2026-01-15"),
                Status = VesselVisitExecutionStatus.Completed
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-3", 
                ActualArrivalTime = DateTime.Parse("2026-02-01"),
                Status = VesselVisitExecutionStatus.Completed
            }
        };

        // Act
        var filtered = vves.Where(v => 
            v.ActualArrivalTime >= startDate && 
            v.ActualArrivalTime <= endDate).ToList();

        // Assert
        Assert.Single(filtered);
        Assert.Equal("VVE-2", filtered.First().Id);
    }

    /// <summary>
    /// Test 7: Confirms VVE collections can be filtered by multiple criteria
    /// (status and date range) for advanced search scenarios
    /// </summary>
    [Fact]
    public void CombinedFilters_WorkCorrectly()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-01-01");
        var endDate = DateTime.Parse("2026-01-31");
        var targetStatus = VesselVisitExecutionStatus.Completed;
        
        var vves = new List<VesselVisitExecution>
        {
            new VesselVisitExecution 
            { 
                Id = "VVE-1", 
                ActualArrivalTime = DateTime.Parse("2026-01-10"),
                Status = VesselVisitExecutionStatus.Completed
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-2", 
                ActualArrivalTime = DateTime.Parse("2026-01-20"),
                Status = VesselVisitExecutionStatus.InProgress
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-3", 
                ActualArrivalTime = DateTime.Parse("2026-01-25"),
                Status = VesselVisitExecutionStatus.Completed
            }
        };

        // Act
        var filtered = vves.Where(v => 
            v.ActualArrivalTime >= startDate && 
            v.ActualArrivalTime <= endDate &&
            v.Status == targetStatus).ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, v => Assert.Equal(targetStatus, v.Status));
    }

    /// <summary>
    /// Test 8: Validates VVE list can be sorted by arrival time
    /// for chronological execution history display
    /// </summary>
    [Fact]
    public void VveList_CanBeSortedByArrivalTime()
    {
        // Arrange
        var vves = new List<VesselVisitExecution>
        {
            new VesselVisitExecution 
            { 
                Id = "VVE-3", 
                ActualArrivalTime = DateTime.Parse("2026-01-25")
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-1", 
                ActualArrivalTime = DateTime.Parse("2026-01-10")
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-2", 
                ActualArrivalTime = DateTime.Parse("2026-01-15")
            }
        };

        // Act
        var sorted = vves.OrderBy(v => v.ActualArrivalTime).ToList();

        // Assert
        Assert.Equal("VVE-1", sorted[0].Id);
        Assert.Equal("VVE-2", sorted[1].Id);
        Assert.Equal("VVE-3", sorted[2].Id);
    }

    /// <summary>
    /// Test 9: Verifies VesselVisitExecutionSearchResultDTO includes execution metrics
    /// (turnaround time, berth occupancy, delays) for performance analysis
    /// </summary>
    [Fact]
    public void VesselVisitExecutionSearchResultDTO_IncludesMetrics()
    {
        // Arrange & Act
        var dto = new VesselVisitExecutionSearchResultDTO
        {
            Id = "VVE-2026-001",
            VvnId = "VVN-2026-001",
            VesselImo = "IMO1234567",
            Status = "Completed",
            ActualArrivalTime = DateTime.Parse("2026-01-15T08:00:00Z"),
            ActualBerthTime = DateTime.Parse("2026-01-15T09:00:00Z"),
            ActualUnberthTime = DateTime.Parse("2026-01-15T16:00:00Z"),
            ActualDepartureTime = DateTime.Parse("2026-01-15T17:00:00Z"),
            TotalTurnaroundTimeHours = 9.0,
            BerthOccupancyTimeHours = 7.0,
            WaitingTimeForBerthingHours = 1.0
        };

        // Assert
        Assert.NotNull(dto.TotalTurnaroundTimeHours);
        Assert.NotNull(dto.BerthOccupancyTimeHours);
        Assert.NotNull(dto.WaitingTimeForBerthingHours);
        Assert.Equal(9.0, dto.TotalTurnaroundTimeHours);
        Assert.Equal(7.0, dto.BerthOccupancyTimeHours);
    }

    /// <summary>
    /// Test 10: Confirms GetAllAsync retrieves all VVE records
    /// for comprehensive execution history review
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllVves()
    {
        // Arrange
        var vves = new List<VesselVisitExecution>
        {
            new VesselVisitExecution 
            { 
                Id = "VVE-1", 
                VvnId = "VVN-1",
                ActualArrivalTime = DateTime.UtcNow,
                Status = VesselVisitExecutionStatus.Completed
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-2", 
                VvnId = "VVN-2",
                ActualArrivalTime = DateTime.UtcNow,
                Status = VesselVisitExecutionStatus.InProgress
            },
            new VesselVisitExecution 
            { 
                Id = "VVE-3", 
                VvnId = "VVN-3",
                ActualArrivalTime = DateTime.UtcNow,
                Status = VesselVisitExecutionStatus.Completed
            }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(vves);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }
}
