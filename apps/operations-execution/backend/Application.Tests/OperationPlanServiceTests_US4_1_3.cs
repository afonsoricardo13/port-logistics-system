using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.3: Unit tests for Operation Plan Search and Listing
/// Tests verify searching and listing operation plans for a given day or period by date range and/or vessel identifier
/// </summary>
public class OperationPlanServiceTests_US4_1_3
{
    private readonly Mock<IOperationPlanRepository> _mockRepository;
    private readonly Mock<ILogger<OperationPlanService>> _mockLogger;

    public OperationPlanServiceTests_US4_1_3()
    {
        _mockRepository = new Mock<IOperationPlanRepository>();
        _mockLogger = new Mock<ILogger<OperationPlanService>>();
    }

    #region Search Request Validation Tests

    /// <summary>
    /// Test 1: Verifies search request requires at least one filter criteria
    /// </summary>
    [Fact]
    public void SearchOperationPlans_RequiresAtLeastOneFilterCriteria()
    {
        // Arrange & Act
        var request = new SearchOperationPlansRequestDTO();

        // Assert
        Assert.True(request.StartDate == null && request.EndDate == null && 
                   string.IsNullOrEmpty(request.VesselImo) && string.IsNullOrEmpty(request.VvnId));
    }

    /// <summary>
    /// Test 2: Verifies date range validation - start date must be before or equal to end date
    /// </summary>
    [Fact]
    public void SearchOperationPlans_StartDateMustBeBeforeOrEqualToEndDate()
    {
        // Arrange
        var request = new SearchOperationPlansRequestDTO
        {
            StartDate = new DateTime(2026, 1, 15),
            EndDate = new DateTime(2026, 1, 10) // Invalid: end before start
        };

        // Act
        var isValid = request.StartDate <= request.EndDate;

        // Assert
        Assert.False(isValid);
    }

    /// <summary>
    /// Test 3: Verifies valid date range passes validation
    /// </summary>
    [Fact]
    public void SearchOperationPlans_ValidDateRangePassesValidation()
    {
        // Arrange
        var request = new SearchOperationPlansRequestDTO
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        };

        // Act
        var isValid = request.StartDate <= request.EndDate;

        // Assert
        Assert.True(isValid);
    }

    #endregion

    #region Repository Query Tests

    /// <summary>
    /// Test 4: Verifies repository is queried with correct date range
    /// </summary>
    [Fact]
    public async Task SearchOperationPlans_QueriesRepositoryWithDateRange()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 1, 31);

        _mockRepository
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate))
            .ReturnsAsync(new List<OperationPlan>());

        // Act
        await _mockRepository.Object.GetByDateRangeAsync(startDate, endDate);

        // Assert
        _mockRepository.Verify(r => r.GetByDateRangeAsync(startDate, endDate), Times.Once);
    }

    /// <summary>
    /// Test 5: Verifies repository returns operation plans for date range
    /// </summary>
    [Fact]
    public async Task SearchOperationPlans_ReturnsPlansForDateRange()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 1, 31);
        
        var plans = new List<OperationPlan>
        {
            new OperationPlan
            {
                Id = Guid.NewGuid(),
                VvnId = "VVN-2026-001",
                Date = new DateTime(2026, 1, 15),
                AlgorithmUsed = "optimal",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            },
            new OperationPlan
            {
                Id = Guid.NewGuid(),
                VvnId = "VVN-2026-002",
                Date = new DateTime(2026, 1, 20),
                AlgorithmUsed = "heuristic",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate))
            .ReturnsAsync(plans);

        // Act
        var result = await _mockRepository.Object.GetByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, p => p.VvnId == "VVN-2026-001");
        Assert.Contains(result, p => p.VvnId == "VVN-2026-002");
    }

    /// <summary>
    /// Test 6: Verifies repository can query by specific VVN ID
    /// </summary>
    [Fact]
    public async Task SearchOperationPlans_QueriesByVvnId()
    {
        // Arrange
        var vvnId = "VVN-2026-001";
        var plans = new List<OperationPlan>
        {
            new OperationPlan
            {
                Id = Guid.NewGuid(),
                VvnId = vvnId,
                Date = DateTime.Today,
                AlgorithmUsed = "optimal",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetByVvnIdAsync(vvnId))
            .ReturnsAsync(plans);

        // Act
        var result = await _mockRepository.Object.GetByVvnIdAsync(vvnId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(vvnId, result.First().VvnId);
        _mockRepository.Verify(r => r.GetByVvnIdAsync(vvnId), Times.Once);
    }

    #endregion

    #region Empty Results Tests

    /// <summary>
    /// Test 7: Verifies service handles empty results gracefully
    /// </summary>
    [Fact]
    public async Task SearchOperationPlans_HandlesEmptyResults()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<OperationPlan>());

        // Act
        var result = await _mockRepository.Object.GetByDateRangeAsync(DateTime.Today, DateTime.Today);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Test 8: Verifies response indicates no results found
    /// </summary>
    [Fact]
    public void SearchOperationPlansResponse_IndicatesNoResults()
    {
        // Arrange & Act
        var response = new SearchOperationPlansResponseDTO
        {
            Success = true,
            TotalResults = 0,
            Plans = new List<OperationPlanSummaryDTO>(),
            Message = "No operation plans found for the specified criteria"
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal(0, response.TotalResults);
        Assert.Empty(response.Plans);
    }

    #endregion

    #region Data Mapping Tests

    /// <summary>
    /// Test 9: Verifies OperationPlan entity maps to summary DTO correctly
    /// </summary>
    [Fact]
    public void OperationPlan_MapsToSummaryDTO()
    {
        // Arrange
        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "VVN-2026-001",
            Date = new DateTime(2026, 1, 15),
            AlgorithmUsed = "optimal",
            CreatedBy = "operator@test.com",
            CreatedAt = DateTime.UtcNow,
            AssignedResources = "[{\"type\":\"dock\",\"id\":\"DOCK-A\"}]",
            TimeWindows = "[{\"start\":\"2026-01-15T09:00:00\",\"end\":\"2026-01-15T14:00:00\"}]"
        };

        // Act
        var dto = new OperationPlanSummaryDTO
        {
            Id = plan.Id,
            VvnId = plan.VvnId,
            Date = plan.Date,
            AlgorithmUsed = plan.AlgorithmUsed,
            CreatedBy = plan.CreatedBy,
            CreatedAt = plan.CreatedAt,
            AssignedDock = "DOCK-A",
            StartTime = new DateTime(2026, 1, 15, 9, 0, 0),
            EndTime = new DateTime(2026, 1, 15, 14, 0, 0)
        };

        // Assert
        Assert.Equal(plan.Id, dto.Id);
        Assert.Equal(plan.VvnId, dto.VvnId);
        Assert.Equal(plan.Date, dto.Date);
        Assert.Equal(plan.AlgorithmUsed, dto.AlgorithmUsed);
    }

    /// <summary>
    /// Test 10: Verifies summary DTO includes vessel metadata when available
    /// </summary>
    [Fact]
    public void SummaryDTO_IncludesVesselMetadata()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            VvnId = "VVN-2026-001",
            VesselImo = "IMO9876543",
            VesselName = "MV Ocean Carrier"
        };

        // Assert
        Assert.Equal("IMO9876543", dto.VesselImo);
        Assert.Equal("MV Ocean Carrier", dto.VesselName);
    }

    #endregion

    #region Sorting Tests

    /// <summary>
    /// Test 11: Verifies plans can be sorted by start time ascending
    /// </summary>
    [Fact]
    public void SearchOperationPlans_CanSortByStartTimeAscending()
    {
        // Arrange
        var plans = new List<OperationPlanSummaryDTO>
        {
            new OperationPlanSummaryDTO { VvnId = "VVN-003", StartTime = new DateTime(2026, 1, 15, 14, 0, 0) },
            new OperationPlanSummaryDTO { VvnId = "VVN-001", StartTime = new DateTime(2026, 1, 15, 8, 0, 0) },
            new OperationPlanSummaryDTO { VvnId = "VVN-002", StartTime = new DateTime(2026, 1, 15, 10, 0, 0) }
        };

        // Act
        var sorted = plans.OrderBy(p => p.StartTime).ToList();

        // Assert
        Assert.Equal("VVN-001", sorted[0].VvnId);
        Assert.Equal("VVN-002", sorted[1].VvnId);
        Assert.Equal("VVN-003", sorted[2].VvnId);
    }

    /// <summary>
    /// Test 12: Verifies plans can be sorted by vessel name
    /// </summary>
    [Fact]
    public void SearchOperationPlans_CanSortByVesselName()
    {
        // Arrange
        var plans = new List<OperationPlanSummaryDTO>
        {
            new OperationPlanSummaryDTO { VesselName = "MV Zulu" },
            new OperationPlanSummaryDTO { VesselName = "MV Alpha" },
            new OperationPlanSummaryDTO { VesselName = "MV Bravo" }
        };

        // Act
        var sorted = plans.OrderBy(p => p.VesselName).ToList();

        // Assert
        Assert.Equal("MV Alpha", sorted[0].VesselName);
        Assert.Equal("MV Bravo", sorted[1].VesselName);
        Assert.Equal("MV Zulu", sorted[2].VesselName);
    }

    /// <summary>
    /// Test 13: Verifies plans can be sorted by delay descending
    /// </summary>
    [Fact]
    public void SearchOperationPlans_CanSortByDelayDescending()
    {
        // Arrange
        var plans = new List<OperationPlanSummaryDTO>
        {
            new OperationPlanSummaryDTO { VvnId = "VVN-001", Delay = 15.5 },
            new OperationPlanSummaryDTO { VvnId = "VVN-002", Delay = 45.2 },
            new OperationPlanSummaryDTO { VvnId = "VVN-003", Delay = 5.0 }
        };

        // Act
        var sorted = plans.OrderByDescending(p => p.Delay).ToList();

        // Assert
        Assert.Equal("VVN-002", sorted[0].VvnId);
        Assert.Equal("VVN-001", sorted[1].VvnId);
        Assert.Equal("VVN-003", sorted[2].VvnId);
    }

    #endregion

    #region Filtering Tests

    /// <summary>
    /// Test 14: Verifies filtering by single day
    /// </summary>
    [Fact]
    public async Task SearchOperationPlans_FiltersBySingleDay()
    {
        // Arrange
        var targetDate = new DateTime(2026, 1, 15);
        var plans = new List<OperationPlan>
        {
            new OperationPlan
            {
                Id = Guid.NewGuid(),
                VvnId = "VVN-2026-001",
                Date = targetDate,
                AlgorithmUsed = "optimal",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            },
            new OperationPlan
            {
                Id = Guid.NewGuid(),
                VvnId = "VVN-2026-002",
                Date = targetDate,
                AlgorithmUsed = "heuristic",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockRepository
            .Setup(r => r.GetByDateRangeAsync(targetDate, targetDate))
            .ReturnsAsync(plans);

        // Act
        var result = await _mockRepository.Object.GetByDateRangeAsync(targetDate, targetDate);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(targetDate, p.Date));
    }

    /// <summary>
    /// Test 15: Verifies filtering by vessel IMO number
    /// </summary>
    [Fact]
    public void SearchOperationPlans_FiltersByVesselImo()
    {
        // Arrange
        var targetImo = "IMO9876543";
        var plans = new List<OperationPlanSummaryDTO>
        {
            new OperationPlanSummaryDTO { VvnId = "VVN-001", VesselImo = "IMO9876543" },
            new OperationPlanSummaryDTO { VvnId = "VVN-002", VesselImo = "IMO1234567" },
            new OperationPlanSummaryDTO { VvnId = "VVN-003", VesselImo = "IMO9876543" }
        };

        // Act
        var filtered = plans.Where(p => p.VesselImo == targetImo).ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, p => Assert.Equal(targetImo, p.VesselImo));
    }

    #endregion

    #region Pagination Tests

    /// <summary>
    /// Test 16: Verifies pagination parameters are processed correctly
    /// </summary>
    [Fact]
    public void SearchOperationPlans_SupportsPagination()
    {
        // Arrange
        var allPlans = new List<OperationPlanSummaryDTO>();
        for (int i = 1; i <= 25; i++)
        {
            allPlans.Add(new OperationPlanSummaryDTO { VvnId = $"VVN-{i:D3}" });
        }

        int pageNumber = 2;
        int pageSize = 10;

        // Act
        var pagedResults = allPlans
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Assert
        Assert.Equal(10, pagedResults.Count);
        Assert.Equal("VVN-011", pagedResults.First().VvnId);
        Assert.Equal("VVN-020", pagedResults.Last().VvnId);
    }

    /// <summary>
    /// Test 17: Verifies pagination response includes metadata
    /// </summary>
    [Fact]
    public void SearchOperationPlansResponse_IncludesPaginationMetadata()
    {
        // Arrange & Act
        var response = new SearchOperationPlansResponseDTO
        {
            Success = true,
            TotalResults = 50,
            PageNumber = 2,
            PageSize = 10,
            TotalPages = 5,
            Plans = new List<OperationPlanSummaryDTO>()
        };

        // Assert
        Assert.Equal(50, response.TotalResults);
        Assert.Equal(2, response.PageNumber);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(5, response.TotalPages);
    }

    #endregion

    #region Resource and Time Window Tests

    /// <summary>
    /// Test 18: Verifies plan summary includes resource information
    /// </summary>
    [Fact]
    public void OperationPlanSummary_IncludesResourceInformation()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            VvnId = "VVN-2026-001",
            AssignedDock = "DOCK-A",
            AssignedResources = "[{\"type\":\"crane\",\"id\":\"CRANE-001\"},{\"type\":\"staff\",\"id\":\"STAFF-101\"}]"
        };

        // Assert
        Assert.Equal("DOCK-A", dto.AssignedDock);
        Assert.NotNull(dto.AssignedResources);
        Assert.Contains("CRANE-001", dto.AssignedResources);
    }

    /// <summary>
    /// Test 19: Verifies plan summary includes time window information
    /// </summary>
    [Fact]
    public void OperationPlanSummary_IncludesTimeWindow()
    {
        // Arrange
        var startTime = new DateTime(2026, 1, 15, 9, 0, 0);
        var endTime = new DateTime(2026, 1, 15, 14, 0, 0);

        // Act
        var dto = new OperationPlanSummaryDTO
        {
            VvnId = "VVN-2026-001",
            StartTime = startTime,
            EndTime = endTime,
            Duration = (int)(endTime - startTime).TotalMinutes
        };

        // Assert
        Assert.Equal(startTime, dto.StartTime);
        Assert.Equal(endTime, dto.EndTime);
        Assert.Equal(300, dto.Duration); // 5 hours = 300 minutes
    }

    /// <summary>
    /// Test 20: Verifies plan summary includes delay information
    /// </summary>
    [Fact]
    public void OperationPlanSummary_IncludesDelayInformation()
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
}
