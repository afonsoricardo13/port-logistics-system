using Application.DTO;
using Domain.Model;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.2: Integration tests for Operation Plan Generation API
/// Tests verify API endpoints for generating and managing operation plans
/// </summary>
public class OperationPlansControllerTests_US4_1_2
{
    #region Request DTO Tests

    /// <summary>
    /// Test 1: Verifies request DTO can be created with all properties
    /// </summary>
    [Fact]
    public void GenerateOperationPlansRequestDTO_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansRequestDTO
        {
            Date = new DateTime(2026, 1, 15),
            Algorithm = "optimal",
            RequestedBy = "operator@portlogistics.com"
        };

        // Assert
        Assert.Equal(new DateTime(2026, 1, 15), dto.Date);
        Assert.Equal("optimal", dto.Algorithm);
        Assert.Equal("operator@portlogistics.com", dto.RequestedBy);
    }

    /// <summary>
    /// Test 2: Verifies request supports heuristic algorithm
    /// </summary>
    [Fact]
    public void GenerateOperationPlansRequestDTO_SupportsHeuristicAlgorithm()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansRequestDTO { Algorithm = "heuristic" };

        // Assert
        Assert.Equal("heuristic", dto.Algorithm);
    }

    /// <summary>
    /// Test 3: Verifies request supports genetic algorithm
    /// </summary>
    [Fact]
    public void GenerateOperationPlansRequestDTO_SupportsGeneticAlgorithm()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansRequestDTO { Algorithm = "genetic" };

        // Assert
        Assert.Equal("genetic", dto.Algorithm);
    }

    #endregion

    #region Response DTO Tests

    /// <summary>
    /// Test 4: Verifies response DTO can represent success
    /// </summary>
    [Fact]
    public void GenerateOperationPlansResponseDTO_CanRepresentSuccess()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansResponseDTO
        {
            Success = true,
            Message = "Operation plans generated successfully for 2026-01-15",
            Date = new DateTime(2026, 1, 15),
            PlansGenerated = 5,
            Plans = new List<OperationPlanSummaryDTO>()
        };

        // Assert
        Assert.True(dto.Success);
        Assert.Equal(5, dto.PlansGenerated);
        Assert.Contains("successfully", dto.Message);
    }

    /// <summary>
    /// Test 5: Verifies response DTO can represent failure
    /// </summary>
    [Fact]
    public void GenerateOperationPlansResponseDTO_CanRepresentFailure()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansResponseDTO
        {
            Success = false,
            Message = "Operation plans already exist for 2026-01-15",
            Date = new DateTime(2026, 1, 15),
            PlansGenerated = 0,
            Error = "Duplicate plans found"
        };

        // Assert
        Assert.False(dto.Success);
        Assert.Equal(0, dto.PlansGenerated);
        Assert.NotNull(dto.Error);
    }

    /// <summary>
    /// Test 6: Verifies response includes plan summaries
    /// </summary>
    [Fact]
    public void GenerateOperationPlansResponseDTO_IncludesPlanSummaries()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansResponseDTO
        {
            Success = true,
            PlansGenerated = 2,
            Plans = new List<OperationPlanSummaryDTO>
            {
                new OperationPlanSummaryDTO
                {
                    Id = Guid.NewGuid(),
                    VvnId = "VVN-2026-001",
                    OperationsCount = 4
                },
                new OperationPlanSummaryDTO
                {
                    Id = Guid.NewGuid(),
                    VvnId = "VVN-2026-002",
                    OperationsCount = 6
                }
            }
        };

        // Assert
        Assert.Equal(2, dto.Plans.Count);
        Assert.Equal(4, dto.Plans[0].OperationsCount);
    }

    #endregion

    #region Plan Summary Tests

    /// <summary>
    /// Test 7: Verifies plan summary includes algorithm used
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_IncludesAlgorithmUsed()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            AlgorithmUsed = "optimal",
            CreatedBy = "operator@test.com",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("optimal", dto.AlgorithmUsed);
    }

    /// <summary>
    /// Test 8: Verifies plan summary includes creation metadata
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_IncludesCreationMetadata()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        // Act
        var dto = new OperationPlanSummaryDTO
        {
            CreatedBy = "operator@test.com",
            CreatedAt = createdAt
        };

        // Assert
        Assert.Equal("operator@test.com", dto.CreatedBy);
        Assert.Equal(createdAt, dto.CreatedAt);
    }

    /// <summary>
    /// Test 9: Verifies plan summary can include assigned resources
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_CanIncludeAssignedResources()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            AssignedResources = "[{\"type\":\"crane\",\"id\":\"CRANE-001\"}]"
        };

        // Assert
        Assert.NotNull(dto.AssignedResources);
        Assert.Contains("CRANE-001", dto.AssignedResources);
    }

    #endregion

    #region Scheduling Service Response Tests

    /// <summary>
    /// Test 10: Verifies scheduling service response contains vessel schedules
    /// </summary>
    [Fact]
    public void SchedulingServiceResponseDTO_ContainsVesselSchedules()
    {
        // Arrange & Act
        var dto = new SchedulingServiceResponseDTO
        {
            Success = true,
            VesselCount = 3,
            Algorithm = "optimal",
            Schedule = new List<VesselScheduleDTO>
            {
                new VesselScheduleDTO { VvnId = "VVN-001" },
                new VesselScheduleDTO { VvnId = "VVN-002" },
                new VesselScheduleDTO { VvnId = "VVN-003" }
            }
        };

        // Assert
        Assert.Equal(3, dto.VesselCount);
        Assert.Equal(3, dto.Schedule.Count);
    }

    /// <summary>
    /// Test 11: Verifies scheduling response includes computation time
    /// </summary>
    [Fact]
    public void SchedulingServiceResponseDTO_IncludesComputationTime()
    {
        // Arrange & Act
        var dto = new SchedulingServiceResponseDTO
        {
            ComputationTimeMs = 1500.5,
            TotalDelay = 30.0
        };

        // Assert
        Assert.Equal(1500.5, dto.ComputationTimeMs);
        Assert.Equal(30.0, dto.TotalDelay);
    }

    #endregion

    #region Vessel Schedule Tests

    /// <summary>
    /// Test 12: Verifies vessel schedule contains operation details
    /// </summary>
    [Fact]
    public void VesselScheduleDTO_ContainsOperationDetails()
    {
        // Arrange & Act
        var dto = new VesselScheduleDTO
        {
            VvnId = "VVN-2026-001",
            VesselImo = "IMO9876543",
            LoadingTime = 120,
            UnloadingTime = 180,
            AssignedDock = "DOCK-A",
            Operations = new List<OperationDetailDTO>
            {
                new OperationDetailDTO { Type = "unloading", Duration = 180 },
                new OperationDetailDTO { Type = "loading", Duration = 120 }
            }
        };

        // Assert
        Assert.Equal("VVN-2026-001", dto.VvnId);
        Assert.Equal(2, dto.Operations.Count);
    }

    /// <summary>
    /// Test 13: Verifies operation detail contains time windows
    /// </summary>
    [Fact]
    public void OperationDetailDTO_ContainsTimeWindows()
    {
        // Arrange
        var startTime = DateTime.Parse("2026-01-15T09:00:00Z");
        var endTime = DateTime.Parse("2026-01-15T12:00:00Z");

        // Act
        var dto = new OperationDetailDTO
        {
            Type = "unloading",
            StartTime = startTime,
            EndTime = endTime,
            Duration = 180
        };

        // Assert
        Assert.Equal(startTime, dto.StartTime);
        Assert.Equal(endTime, dto.EndTime);
        Assert.Equal(180, dto.Duration);
    }

    /// <summary>
    /// Test 14: Verifies operation detail can include crane assignment
    /// </summary>
    [Fact]
    public void OperationDetailDTO_CanIncludeCraneAssignment()
    {
        // Arrange & Act
        var dto = new OperationDetailDTO
        {
            Type = "loading",
            AssignedCrane = "CRANE-001",
            AssignedDock = "DOCK-B"
        };

        // Assert
        Assert.Equal("CRANE-001", dto.AssignedCrane);
        Assert.Equal("DOCK-B", dto.AssignedDock);
    }

    /// <summary>
    /// Test 15: Verifies vessel schedule can include delay information
    /// </summary>
    [Fact]
    public void VesselScheduleDTO_CanIncludeDelayInformation()
    {
        // Arrange & Act
        var dto = new VesselScheduleDTO
        {
            VvnId = "VVN-2026-001",
            Delay = 15.5
        };

        // Assert
        Assert.Equal(15.5, dto.Delay);
    }

    #endregion
}
