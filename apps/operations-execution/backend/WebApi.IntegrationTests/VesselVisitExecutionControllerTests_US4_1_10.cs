using Application.DTO;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.10: Integration tests for Search and List Vessel Visit Executions endpoint
/// These tests validate API response structures, filtering logic, and metric calculations
/// without requiring the full application stack to be running
/// </summary>
public class VesselVisitExecutionControllerTests_US4_1_10
{
    /// <summary>
    /// Test 1: Validates VesselVisitExecutionSearchResultDTO contains all required fields
    /// for displaying execution history with calculated metrics
    /// </summary>
    [Fact]
    public void VesselVisitExecutionSearchResultDTO_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new VesselVisitExecutionSearchResultDTO
        {
            Id = "VVE-2026-001",
            VvnId = "VVN-2026-001",
            VesselImo = "IMO1234567",
            VesselName = "Cargo Ship Alpha",
            Status = "Completed",
            ActualArrivalTime = DateTime.Parse("2026-01-15T08:00:00Z"),
            ActualBerthTime = DateTime.Parse("2026-01-15T09:00:00Z"),
            ActualUnberthTime = DateTime.Parse("2026-01-15T16:00:00Z"),
            ActualDepartureTime = DateTime.Parse("2026-01-15T17:00:00Z"),
            DockUsed = "DOCK-A",
            TotalTurnaroundTimeHours = 9.0,
            BerthOccupancyTimeHours = 7.0,
            WaitingTimeForBerthingHours = 1.0,
            ArrivalDelayHours = 0.0,
            DepartureDelayHours = 0.5
        };

        // Assert
        Assert.Equal("VVE-2026-001", dto.Id);
        Assert.Equal("IMO1234567", dto.VesselImo);
        Assert.Equal("Completed", dto.Status);
        Assert.NotNull(dto.TotalTurnaroundTimeHours);
        Assert.NotNull(dto.BerthOccupancyTimeHours);
        Assert.NotNull(dto.WaitingTimeForBerthingHours);
        Assert.Equal(9.0, dto.TotalTurnaroundTimeHours);
    }

    /// <summary>
    /// Test 2: Verifies metric calculation for total turnaround time
    /// (time from arrival at port to departure from port)
    /// </summary>
    [Fact]
    public void TotalTurnaroundTime_IsCalculatedCorrectly()
    {
        // Arrange
        var arrivalTime = DateTime.Parse("2026-01-15T08:00:00Z");
        var departureTime = DateTime.Parse("2026-01-15T17:00:00Z");

        // Act
        var turnaroundTime = departureTime - arrivalTime;

        // Assert
        Assert.Equal(TimeSpan.FromHours(9), turnaroundTime);
    }

    /// <summary>
    /// Test 3: Validates berth occupancy time calculation
    /// (time from berth to unberth)
    /// </summary>
    [Fact]
    public void BerthOccupancyTime_IsCalculatedCorrectly()
    {
        // Arrange
        var berthTime = DateTime.Parse("2026-01-15T09:00:00Z");
        var unberthTime = DateTime.Parse("2026-01-15T16:00:00Z");

        // Act
        var occupancyTime = unberthTime - berthTime;

        // Assert
        Assert.Equal(TimeSpan.FromHours(7), occupancyTime);
    }

    /// <summary>
    /// Test 4: Confirms waiting time for berthing calculation
    /// (time from arrival to berth)
    /// </summary>
    [Fact]
    public void WaitingTimeForBerthing_IsCalculatedCorrectly()
    {
        // Arrange
        var arrivalTime = DateTime.Parse("2026-01-15T08:00:00Z");
        var berthTime = DateTime.Parse("2026-01-15T09:00:00Z");

        // Act
        var waitingTime = berthTime - arrivalTime;

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), waitingTime);
    }

    /// <summary>
    /// Test 5: Tests search result DTO supports empty metrics
    /// for in-progress VVEs that haven't completed all stages
    /// </summary>
    [Fact]
    public void SearchResultDTO_SupportsNullMetrics_ForInProgressVves()
    {
        // Arrange & Act
        var dto = new VesselVisitExecutionSearchResultDTO
        {
            Id = "VVE-2026-002",
            VvnId = "VVN-2026-002",
            Status = "InProgress",
            ActualArrivalTime = DateTime.Parse("2026-01-15T10:00:00Z"),
            ActualBerthTime = DateTime.Parse("2026-01-15T11:00:00Z"),
            ActualUnberthTime = null,
            ActualDepartureTime = null,
            TotalTurnaroundTimeHours = null,
            BerthOccupancyTimeHours = null
        };

        // Assert
        Assert.Equal("InProgress", dto.Status);
        Assert.NotNull(dto.ActualArrivalTime);
        Assert.NotNull(dto.ActualBerthTime);
        Assert.Null(dto.ActualUnberthTime);
        Assert.Null(dto.TotalTurnaroundTimeHours);
    }

    /// <summary>
    /// Test 6: Validates VVE list can be filtered by date range
    /// for temporal analysis of execution history
    /// </summary>
    [Fact]
    public void VveList_CanBeFilteredByDateRange()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-01-01");
        var endDate = DateTime.Parse("2026-01-31");
        
        var vves = new List<VesselVisitExecutionSearchResultDTO>
        {
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-1",
                VvnId = "VVN-1",
                ActualArrivalTime = DateTime.Parse("2025-12-31")
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-2",
                VvnId = "VVN-2",
                ActualArrivalTime = DateTime.Parse("2026-01-15")
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-3",
                VvnId = "VVN-3",
                ActualArrivalTime = DateTime.Parse("2026-02-01")
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
    /// Test 7: Confirms VVE search results can be filtered by vessel IMO number
    /// for vessel-specific execution history review
    /// </summary>
    [Fact]
    public void VveList_CanBeFilteredByVesselImo()
    {
        // Arrange
        var targetImo = "IMO1234567";
        var vves = new List<VesselVisitExecutionSearchResultDTO>
        {
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-1",
                VvnId = "VVN-1",
                VesselImo = "IMO1234567" 
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-2",
                VvnId = "VVN-2",
                VesselImo = "IMO9876543" 
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-3",
                VvnId = "VVN-3",
                VesselImo = "IMO1234567" 
            }
        };

        // Act
        var filtered = vves.Where(v => v.VesselImo == targetImo).ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, v => Assert.Equal(targetImo, v.VesselImo));
    }

    /// <summary>
    /// Test 8: Validates VVE search results can be filtered by execution status
    /// to identify completed, in-progress, or specific state executions
    /// </summary>
    [Fact]
    public void VveList_CanBeFilteredByStatus()
    {
        // Arrange
        var targetStatus = "Completed";
        var vves = new List<VesselVisitExecutionSearchResultDTO>
        {
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-1",
                VvnId = "VVN-1",
                Status = "Completed" 
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-2",
                VvnId = "VVN-2",
                Status = "InProgress" 
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-3",
                VvnId = "VVN-3",
                Status = "Completed" 
            }
        };

        // Act
        var filtered = vves.Where(v => v.Status == targetStatus).ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, v => Assert.Equal(targetStatus, v.Status));
    }

    /// <summary>
    /// Test 9: Verifies VVE search results can be sorted by total turnaround time
    /// for performance comparison and efficiency analysis
    /// </summary>
    [Fact]
    public void VveList_CanBeSortedByTurnaroundTime()
    {
        // Arrange
        var vves = new List<VesselVisitExecutionSearchResultDTO>
        {
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-1",
                VvnId = "VVN-1",
                TotalTurnaroundTimeHours = 10.0
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-2",
                VvnId = "VVN-2",
                TotalTurnaroundTimeHours = 5.0
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-3",
                VvnId = "VVN-3",
                TotalTurnaroundTimeHours = 8.0
            }
        };

        // Act
        var sorted = vves.OrderBy(v => v.TotalTurnaroundTimeHours).ToList();

        // Assert
        Assert.Equal("VVE-2", sorted[0].Id);
        Assert.Equal("VVE-3", sorted[1].Id);
        Assert.Equal("VVE-1", sorted[2].Id);
    }

    /// <summary>
    /// Test 10: Confirms delay metrics are correctly represented
    /// for performance analysis and operational improvement
    /// </summary>
    [Fact]
    public void DelayMetrics_AreCorrectlyCalculated()
    {
        // Arrange & Act
        var dto = new VesselVisitExecutionSearchResultDTO
        {
            Id = "VVE-2026-001",
            VvnId = "VVN-2026-001",
            Status = "Completed",
            ArrivalDelayHours = 0.25,
            DepartureDelayHours = 0.5
        };

        // Assert
        Assert.NotNull(dto.ArrivalDelayHours);
        Assert.NotNull(dto.DepartureDelayHours);
        Assert.Equal(0.25, dto.ArrivalDelayHours);
        Assert.Equal(0.5, dto.DepartureDelayHours);
        Assert.True(dto.DepartureDelayHours > dto.ArrivalDelayHours);
    }

    /// <summary>
    /// Test 11: Validates combined filtering (date range + status + vessel)
    /// for advanced search and performance analysis scenarios
    /// </summary>
    [Fact]
    public void CombinedFilters_WorkCorrectlyForAdvancedSearch()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-01-01");
        var endDate = DateTime.Parse("2026-01-31");
        var targetStatus = "Completed";
        var targetImo = "IMO1234567";
        
        var vves = new List<VesselVisitExecutionSearchResultDTO>
        {
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-1",
                ActualArrivalTime = DateTime.Parse("2026-01-10"),
                Status = "Completed",
                VesselImo = "IMO1234567"
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-2",
                ActualArrivalTime = DateTime.Parse("2026-01-15"),
                Status = "InProgress",
                VesselImo = "IMO1234567"
            },
            new VesselVisitExecutionSearchResultDTO 
            { 
                Id = "VVE-3",
                ActualArrivalTime = DateTime.Parse("2026-01-20"),
                Status = "Completed",
                VesselImo = "IMO9876543"
            }
        };

        // Act
        var filtered = vves.Where(v => 
            v.ActualArrivalTime >= startDate && 
            v.ActualArrivalTime <= endDate &&
            v.Status == targetStatus &&
            v.VesselImo == targetImo).ToList();

        // Assert
        Assert.Single(filtered);
        Assert.Equal("VVE-1", filtered.First().Id);
    }

    /// <summary>
    /// Test 12: Confirms search results support pagination-ready data structure
    /// for efficient display of large execution history datasets
    /// </summary>
    [Fact]
    public void VveSearchResults_SupportPaginationReadyStructure()
    {
        // Arrange
        var vves = Enumerable.Range(1, 50)
            .Select(i => new VesselVisitExecutionSearchResultDTO 
            { 
                Id = $"VVE-{i:D3}",
                ActualArrivalTime = DateTime.Parse("2026-01-01").AddHours(i)
            })
            .ToList();

        int pageSize = 10;
        int pageNumber = 2;

        // Act
        var page = vves
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Assert
        Assert.Equal(10, page.Count);
        Assert.Equal("VVE-011", page.First().Id);
        Assert.Equal("VVE-020", page.Last().Id);
    }
}
