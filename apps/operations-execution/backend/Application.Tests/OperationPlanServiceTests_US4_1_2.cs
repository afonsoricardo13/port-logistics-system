using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.2: Unit tests for Operation Plan Generation
/// Tests verify generation, storage, and management of operation plans for VVNs scheduled for a given day
/// </summary>
public class OperationPlanServiceTests_US4_1_2
{
    private readonly Mock<IOperationPlanRepository> _mockRepository;
    private readonly Mock<ILogger<OperationPlanService>> _mockLogger;

    public OperationPlanServiceTests_US4_1_2()
    {
        _mockRepository = new Mock<IOperationPlanRepository>();
        _mockLogger = new Mock<ILogger<OperationPlanService>>();
    }

    #region Request DTO Tests

    /// <summary>
    /// Test 1: Verifies GenerateOperationPlansRequestDTO contains required fields
    /// </summary>
    [Fact]
    public void GenerateOperationPlansRequestDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansRequestDTO
        {
            Date = DateTime.Today,
            Algorithm = "optimal",
            RequestedBy = "operator@test.com"
        };

        // Assert
        Assert.Equal(DateTime.Today, dto.Date);
        Assert.Equal("optimal", dto.Algorithm);
        Assert.Equal("operator@test.com", dto.RequestedBy);
    }

    /// <summary>
    /// Test 2: Verifies default algorithm is "optimal"
    /// </summary>
    [Fact]
    public void GenerateOperationPlansRequestDTO_DefaultAlgorithmIsOptimal()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansRequestDTO();

        // Assert
        Assert.Equal("optimal", dto.Algorithm);
    }

    /// <summary>
    /// Test 3: Verifies supported algorithms
    /// </summary>
    [Theory]
    [InlineData("optimal")]
    [InlineData("heuristic")]
    [InlineData("genetic")]
    [InlineData("multi_crane")]
    public void GenerateOperationPlansRequestDTO_SupportsMultipleAlgorithms(string algorithm)
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansRequestDTO { Algorithm = algorithm };

        // Assert
        Assert.Equal(algorithm, dto.Algorithm);
    }

    #endregion

    #region Response DTO Tests

    /// <summary>
    /// Test 4: Verifies GenerateOperationPlansResponseDTO contains required fields
    /// </summary>
    [Fact]
    public void GenerateOperationPlansResponseDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansResponseDTO
        {
            Success = true,
            Message = "Plans generated successfully",
            Date = DateTime.Today,
            PlansGenerated = 5
        };

        // Assert
        Assert.True(dto.Success);
        Assert.Equal("Plans generated successfully", dto.Message);
        Assert.Equal(DateTime.Today, dto.Date);
        Assert.Equal(5, dto.PlansGenerated);
    }

    /// <summary>
    /// Test 5: Verifies response can contain error information
    /// </summary>
    [Fact]
    public void GenerateOperationPlansResponseDTO_CanContainError()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansResponseDTO
        {
            Success = false,
            Message = "Generation failed",
            Error = "No approved VVNs found"
        };

        // Assert
        Assert.False(dto.Success);
        Assert.NotNull(dto.Error);
    }

    /// <summary>
    /// Test 6: Verifies response contains plan summaries list
    /// </summary>
    [Fact]
    public void GenerateOperationPlansResponseDTO_ContainsPlansList()
    {
        // Arrange & Act
        var dto = new GenerateOperationPlansResponseDTO
        {
            Plans = new List<OperationPlanSummaryDTO>
            {
                new OperationPlanSummaryDTO { VvnId = "VVN-001" },
                new OperationPlanSummaryDTO { VvnId = "VVN-002" }
            }
        };

        // Assert
        Assert.Equal(2, dto.Plans.Count);
    }

    #endregion

    #region Operation Plan Summary Tests

    /// <summary>
    /// Test 7: Verifies OperationPlanSummaryDTO contains all fields
    /// </summary>
    [Fact]
    public void OperationPlanSummaryDTO_ContainsAllFields()
    {
        // Arrange & Act
        var dto = new OperationPlanSummaryDTO
        {
            Id = Guid.NewGuid(),
            VvnId = "VVN-2026-001",
            Date = DateTime.Today,
            AlgorithmUsed = "optimal",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "operator@test.com",
            OperationsCount = 4
        };

        // Assert
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("VVN-2026-001", dto.VvnId);
        Assert.Equal("optimal", dto.AlgorithmUsed);
        Assert.Equal(4, dto.OperationsCount);
    }

    #endregion

    #region Repository Tests

    /// <summary>
    /// Test 8: Verifies GetByDateAsync returns existing plans
    /// </summary>
    [Fact]
    public async Task Repository_GetByDateAsync_ReturnsExistingPlans()
    {
        // Arrange
        var date = DateTime.Today;
        var plans = new List<OperationPlan>
        {
            new OperationPlan { Id = Guid.NewGuid(), VvnId = "VVN-001", Date = date },
            new OperationPlan { Id = Guid.NewGuid(), VvnId = "VVN-002", Date = date }
        };

        _mockRepository.Setup(r => r.GetByDateAsync(date)).ReturnsAsync(plans);

        // Act
        var result = await _mockRepository.Object.GetByDateAsync(date);

        // Assert
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    /// Test 9: Verifies GetByDateAsync returns empty when no plans exist
    /// </summary>
    [Fact]
    public async Task Repository_GetByDateAsync_ReturnsEmptyWhenNoPlans()
    {
        // Arrange
        var date = DateTime.Today;
        _mockRepository.Setup(r => r.GetByDateAsync(date)).ReturnsAsync(new List<OperationPlan>());

        // Act
        var result = await _mockRepository.Object.GetByDateAsync(date);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Test 10: Verifies GetByVvnIdAsync returns plan for specific VVN
    /// </summary>
    [Fact]
    public async Task Repository_GetByVvnIdAsync_ReturnsPlanForVvn()
    {
        // Arrange
        var vvnId = "VVN-2026-001";
        var plan = new OperationPlan { Id = Guid.NewGuid(), VvnId = vvnId };

        _mockRepository.Setup(r => r.GetByVvnIdAsync(vvnId)).ReturnsAsync(new List<OperationPlan> { plan });

        // Act
        var result = await _mockRepository.Object.GetByVvnIdAsync(vvnId);

        // Assert
        Assert.Single(result);
        Assert.Equal(vvnId, result.First().VvnId);
    }

    #endregion

    #region Operation Plan Model Tests

    /// <summary>
    /// Test 11: Verifies OperationPlan model contains required properties
    /// </summary>
    [Fact]
    public void OperationPlan_ContainsRequiredProperties()
    {
        // Arrange & Act
        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "VVN-2026-001",
            Date = DateTime.Today,
            Operations = "[{\"type\":\"unloading\"}]",
            AssignedResources = "[{\"type\":\"crane\"}]",
            TimeWindows = "[{\"start\":\"09:00\"}]",
            CreatedBy = "operator@test.com",
            CreatedAt = DateTime.UtcNow,
            AlgorithmUsed = "optimal"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal("VVN-2026-001", plan.VvnId);
        Assert.Contains("unloading", plan.Operations);
        Assert.Contains("crane", plan.AssignedResources);
    }

    /// <summary>
    /// Test 12: Verifies OperationPlan tracks creation metadata
    /// </summary>
    [Fact]
    public void OperationPlan_TracksCreationMetadata()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var createdBy = "operator@test.com";

        // Act
        var plan = new OperationPlan
        {
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };

        // Assert
        Assert.Equal(createdBy, plan.CreatedBy);
        Assert.Equal(createdAt, plan.CreatedAt);
    }

    /// <summary>
    /// Test 13: Verifies OperationPlan stores algorithm used
    /// </summary>
    [Fact]
    public void OperationPlan_StoresAlgorithmUsed()
    {
        // Arrange & Act
        var plan = new OperationPlan { AlgorithmUsed = "genetic" };

        // Assert
        Assert.Equal("genetic", plan.AlgorithmUsed);
    }

    /// <summary>
    /// Test 14: Verifies OperationPlan can store optional metadata
    /// </summary>
    [Fact]
    public void OperationPlan_CanStoreOptionalMetadata()
    {
        // Arrange & Act
        var plan = new OperationPlan
        {
            Metadata = "{\"computationTime\": 1500}"
        };

        // Assert
        Assert.NotNull(plan.Metadata);
        Assert.Contains("computationTime", plan.Metadata);
    }

    /// <summary>
    /// Test 15: Verifies OperationPlan tracks updates (US 4.1.4 related)
    /// </summary>
    [Fact]
    public void OperationPlan_TracksUpdates()
    {
        // Arrange & Act
        var plan = new OperationPlan
        {
            UpdatedBy = "manager@test.com",
            UpdatedAt = DateTime.UtcNow,
            ChangeLogs = "[{\"change\":\"Modified time window\"}]"
        };

        // Assert
        Assert.NotNull(plan.UpdatedBy);
        Assert.NotNull(plan.UpdatedAt);
        Assert.NotNull(plan.ChangeLogs);
    }

    #endregion
}
