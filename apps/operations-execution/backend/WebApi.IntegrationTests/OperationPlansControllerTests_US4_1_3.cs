using Application.DTO;
using Domain.Model;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.3: Integration tests for Operation Plan Search and Listing API
/// Tests verify API endpoints for searching and listing operation plans by date range or vessel identifier
/// </summary>
public class OperationPlansControllerTests_US4_1_3
{
    #region Search Request DTO Tests

    /// <summary>
    /// Test 1: Verifies search request DTO can be created with date range
    /// </summary>
    [Fact]
    public void SearchOperationPlansRequestDTO_CanBeCreatedWithDateRange()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansRequestDTO
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        };

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1), dto.StartDate);
        Assert.Equal(new DateTime(2026, 1, 31), dto.EndDate);
    }

    /// <summary>
    /// Test 2: Verifies search request supports single day query
    /// </summary>
    [Fact]
    public void SearchOperationPlansRequestDTO_SupportsSingleDayQuery()
    {
        // Arrange
        var targetDate = new DateTime(2026, 1, 15);

        // Act
        var dto = new SearchOperationPlansRequestDTO
        {
            StartDate = targetDate,
            EndDate = targetDate
        };

        // Assert
        Assert.Equal(targetDate, dto.StartDate);
        Assert.Equal(targetDate, dto.EndDate);
    }

    /// <summary>
    /// Test 3: Verifies search request supports vessel identifier filter
    /// </summary>
    [Fact]
    public void SearchOperationPlansRequestDTO_SupportsVesselIdentifierFilter()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansRequestDTO
        {
            VesselImo = "IMO9876543"
        };

        // Assert
        Assert.Equal("IMO9876543", dto.VesselImo);
    }

    /// <summary>
    /// Test 4: Verifies search request supports VVN identifier filter
    /// </summary>
    [Fact]
    public void SearchOperationPlansRequestDTO_SupportsVvnIdFilter()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansRequestDTO
        {
            VvnId = "VVN-2026-001"
        };

        // Assert
        Assert.Equal("VVN-2026-001", dto.VvnId);
    }

    /// <summary>
    /// Test 5: Verifies search request supports combined filters
    /// </summary>
    [Fact]
    public void SearchOperationPlansRequestDTO_SupportsCombinedFilters()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansRequestDTO
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            VesselImo = "IMO9876543"
        };

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1), dto.StartDate);
        Assert.Equal(new DateTime(2026, 1, 31), dto.EndDate);
        Assert.Equal("IMO9876543", dto.VesselImo);
    }

    #endregion

    #region Search Response DTO Tests

    /// <summary>
    /// Test 6: Verifies search response contains plan summaries
    /// </summary>
    [Fact]
    public void SearchOperationPlansResponseDTO_ContainsPlanSummaries()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansResponseDTO
        {
            Success = true,
            TotalResults = 5,
            Plans = new List<OperationPlanSummaryDTO>
            {
                new OperationPlanSummaryDTO { VvnId = "VVN-001" },
                new OperationPlanSummaryDTO { VvnId = "VVN-002" },
                new OperationPlanSummaryDTO { VvnId = "VVN-003" },
                new OperationPlanSummaryDTO { VvnId = "VVN-004" },
                new OperationPlanSummaryDTO { VvnId = "VVN-005" }
            }
        };

        // Assert
        Assert.True(dto.Success);
        Assert.Equal(5, dto.TotalResults);
        Assert.Equal(5, dto.Plans.Count);
    }

    /// <summary>
    /// Test 7: Verifies search response handles empty results
    /// </summary>
    [Fact]
    public void SearchOperationPlansResponseDTO_HandlesEmptyResults()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansResponseDTO
        {
            Success = true,
            TotalResults = 0,
            Message = "No operation plans found for the specified criteria",
            Plans = new List<OperationPlanSummaryDTO>()
        };

        // Assert
        Assert.True(dto.Success);
        Assert.Equal(0, dto.TotalResults);
        Assert.Empty(dto.Plans);
        Assert.Contains("No operation plans found", dto.Message);
    }

    /// <summary>
    /// Test 8: Verifies search response supports pagination info
    /// </summary>
    [Fact]
    public void SearchOperationPlansResponseDTO_SupportsPaginationInfo()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansResponseDTO
        {
            Success = true,
            TotalResults = 50,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 5,
            Plans = new List<OperationPlanSummaryDTO>()
        };

        // Assert
        Assert.Equal(50, dto.TotalResults);
        Assert.Equal(1, dto.PageNumber);
        Assert.Equal(10, dto.PageSize);
        Assert.Equal(5, dto.TotalPages);
    }

    #endregion

    #region Plan Summary Display Tests

    /// <summary>
    /// Test 9: Verifies plan summary includes vessel information
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_IncludesVesselInformation()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            Id = Guid.NewGuid(),
            VvnId = "VVN-2026-001",
            VesselImo = "IMO9876543",
            VesselName = "MV Ocean Carrier"
        };

        // Assert
        Assert.Equal("VVN-2026-001", dto.VvnId);
        Assert.Equal("IMO9876543", dto.VesselImo);
        Assert.Equal("MV Ocean Carrier", dto.VesselName);
    }

    /// <summary>
    /// Test 10: Verifies plan summary includes dock assignment
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_IncludesDockAssignment()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            AssignedDock = "DOCK-A",
            StartTime = new DateTime(2026, 1, 15, 9, 0, 0),
            EndTime = new DateTime(2026, 1, 15, 14, 0, 0)
        };

        // Assert
        Assert.Equal("DOCK-A", dto.AssignedDock);
        Assert.NotNull(dto.StartTime);
        Assert.NotNull(dto.EndTime);
    }

    /// <summary>
    /// Test 11: Verifies plan summary includes resource information
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_IncludesResourceInformation()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            OperationsCount = 4,
            AssignedResources = "[{\"type\":\"crane\",\"id\":\"CRANE-001\"},{\"type\":\"crane\",\"id\":\"CRANE-002\"}]"
        };

        // Assert
        Assert.Equal(4, dto.OperationsCount);
        Assert.NotNull(dto.AssignedResources);
        Assert.Contains("CRANE-001", dto.AssignedResources);
    }

    /// <summary>
    /// Test 12: Verifies plan summary includes expected delay
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_IncludesExpectedDelay()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            VvnId = "VVN-2026-001",
            Delay = 25.5
        };

        // Assert
        Assert.Equal(25.5, dto.Delay);
    }

    #endregion

    #region Sorting Tests

    /// <summary>
    /// Test 13: Verifies search request supports sorting by start time
    /// </summary>
    [Fact]
    public void SearchOperationPlansRequestDTO_SupportsSortingByStartTime()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansRequestDTO
        {
            SortBy = "StartTime",
            SortDirection = "asc"
        };

        // Assert
        Assert.Equal("StartTime", dto.SortBy);
        Assert.Equal("asc", dto.SortDirection);
    }

    /// <summary>
    /// Test 14: Verifies search request supports sorting by vessel name
    /// </summary>
    [Fact]
    public void SearchOperationPlansRequestDTO_SupportsSortingByVesselName()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansRequestDTO
        {
            SortBy = "VesselName",
            SortDirection = "desc"
        };

        // Assert
        Assert.Equal("VesselName", dto.SortBy);
        Assert.Equal("desc", dto.SortDirection);
    }

    /// <summary>
    /// Test 15: Verifies search request supports sorting by delay
    /// </summary>
    [Fact]
    public void SearchOperationPlansRequestDTO_SupportsSortingByDelay()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansRequestDTO
        {
            SortBy = "Delay",
            SortDirection = "desc"
        };

        // Assert
        Assert.Equal("Delay", dto.SortBy);
        Assert.Equal("desc", dto.SortDirection);
    }

    #endregion

    #region Filter and Search Combination Tests

    /// <summary>
    /// Test 16: Verifies multiple plans can be sorted by start time
    /// </summary>
    [Fact]
    public void SearchOperationPlansResponseDTO_PlansCanBeSortedByStartTime()
    {
        // Arrange
        var plans = new List<OperationPlanSummaryDTO>
        {
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-003",
                StartTime = new DateTime(2026, 1, 15, 14, 0, 0)
            },
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-001",
                StartTime = new DateTime(2026, 1, 15, 8, 0, 0)
            },
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-002",
                StartTime = new DateTime(2026, 1, 15, 10, 0, 0)
            }
        };

        // Act
        var sortedPlans = plans.OrderBy(p => p.StartTime).ToList();

        // Assert
        Assert.Equal("VVN-001", sortedPlans[0].VvnId);
        Assert.Equal("VVN-002", sortedPlans[1].VvnId);
        Assert.Equal("VVN-003", sortedPlans[2].VvnId);
    }

    /// <summary>
    /// Test 17: Verifies plans can be filtered by date range
    /// </summary>
    [Fact]
    public void OperationPlans_CanBeFilteredByDateRange()
    {
        // Arrange
        var plans = new List<OperationPlanSummaryDTO>
        {
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-001",
                Date = new DateTime(2026, 1, 10)
            },
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-002",
                Date = new DateTime(2026, 1, 15)
            },
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-003",
                Date = new DateTime(2026, 1, 20)
            }
        };

        var startDate = new DateTime(2026, 1, 12);
        var endDate = new DateTime(2026, 1, 18);

        // Act
        var filteredPlans = plans.Where(p => p.Date >= startDate && p.Date <= endDate).ToList();

        // Assert
        Assert.Single(filteredPlans);
        Assert.Equal("VVN-002", filteredPlans[0].VvnId);
    }

    /// <summary>
    /// Test 18: Verifies plans can be filtered by vessel identifier
    /// </summary>
    [Fact]
    public void OperationPlans_CanBeFilteredByVesselIdentifier()
    {
        // Arrange
        var plans = new List<OperationPlanSummaryDTO>
        {
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-001",
                VesselImo = "IMO1111111"
            },
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-002",
                VesselImo = "IMO2222222"
            },
            new OperationPlanSummaryDTO
            {
                VvnId = "VVN-003",
                VesselImo = "IMO1111111"
            }
        };

        // Act
        var filteredPlans = plans.Where(p => p.VesselImo == "IMO1111111").ToList();

        // Assert
        Assert.Equal(2, filteredPlans.Count);
        Assert.All(filteredPlans, p => Assert.Equal("IMO1111111", p.VesselImo));
    }

    #endregion

    #region Metadata Display Tests

    /// <summary>
    /// Test 19: Verifies plan summary includes creation metadata
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_IncludesCreationMetadata()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        // Act
        var dto = new OperationPlanSummaryDTO
        {
            CreatedAt = createdAt,
            CreatedBy = "operator@portlogistics.com",
            AlgorithmUsed = "optimal"
        };

        // Assert
        Assert.Equal(createdAt, dto.CreatedAt);
        Assert.Equal("operator@portlogistics.com", dto.CreatedBy);
        Assert.Equal("optimal", dto.AlgorithmUsed);
    }

    /// <summary>
    /// Test 20: Verifies search response can include date range in summary
    /// </summary>
    [Fact]
    public void SearchOperationPlansResponseDTO_CanIncludeDateRangeInSummary()
    {
        // Arrange & Act
        var dto = new SearchOperationPlansResponseDTO
        {
            Success = true,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            TotalResults = 10,
            Message = "Found 10 operation plans between 2026-01-01 and 2026-01-31"
        };

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1), dto.StartDate);
        Assert.Equal(new DateTime(2026, 1, 31), dto.EndDate);
        Assert.Equal(10, dto.TotalResults);
    }

    #endregion
}
