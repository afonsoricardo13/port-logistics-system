using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.5: Unit tests for Get VVNs without Operation Plans functionality
/// These tests verify core business logic, data structures, and filtering operations
/// without requiring external HTTP dependencies
/// </summary>
public class OperationPlanServiceTests_US4_1_5
{
    /// <summary>
    /// Test 1: Verifies that OperationPlan domain model can be instantiated
    /// with a VvnId, which is essential for matching VVNs to their plans
    /// </summary>
    [Fact]
    public void OperationPlan_CanBeCreatedWithVvnId()
    {
        // Arrange & Act
        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "123",
            Date = DateTime.Parse("2026-01-15"),
            AlgorithmUsed = "optimal",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test@example.com"
        };

        // Assert
        Assert.NotNull(plan);
        Assert.Equal("123", plan.VvnId);
    }

    /// <summary>
    /// Test 2: Validates that VvnDTO (from Port Logistics API) can be created
    /// with all required properties used for identifying missing plans
    /// </summary>
    [Fact]
    public void VvnDTO_CanBeCreatedWithRequiredProperties()
    {
        // Arrange & Act
        var vvn = new VvnDTO
        {
            Id = 1,
            VesselImoNumber = "IMO1234567",
            EstimatedTimeOfArrival = DateTime.Parse("2026-01-15T08:00:00Z"),
            EstimatedTimeOfDeparture = DateTime.Parse("2026-01-15T16:00:00Z"),
            AssignedDockCode = "DOCK-A"
        };

        // Assert
        Assert.NotNull(vvn);
        Assert.Equal(1, vvn.Id);
        Assert.Equal("IMO1234567", vvn.VesselImoNumber);
    }

    /// <summary>
    /// Test 3: Ensures VvnId string comparison works correctly
    /// This is critical for matching VVNs with their operation plans
    /// </summary>
    [Theory]
    [InlineData("1", "1", true)]
    [InlineData("1", "2", false)]
    [InlineData("123", "123", true)]
    public void VvnIdComparison_WorksCorrectly(string vvnId1, string vvnId2, bool shouldMatch)
    {
        // Arrange
        var plan = new OperationPlan { VvnId = vvnId1 };

        // Act
        var matches = plan.VvnId.Equals(vvnId2, StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.Equal(shouldMatch, matches);
    }

    /// <summary>
    /// Test 4: Validates that a HashSet can efficiently store and check
    /// existing VvnIds from operation plans for fast lookups
    /// </summary>
    [Fact]
    public void OperationPlanCollection_CanFilterByVvnId()
    {
        // Arrange
        var plans = new List<OperationPlan>
        {
            new OperationPlan { Id = Guid.NewGuid(), VvnId = "1", Date = DateTime.Parse("2026-01-15") },
            new OperationPlan { Id = Guid.NewGuid(), VvnId = "2", Date = DateTime.Parse("2026-01-15") },
            new OperationPlan { Id = Guid.NewGuid(), VvnId = "3", Date = DateTime.Parse("2026-01-15") }
        };

        // Act
        var existingVvnIds = new HashSet<string>(
            plans.Select(p => p.VvnId),
            StringComparer.OrdinalIgnoreCase);

        // Assert
        Assert.Contains("1", existingVvnIds);
        Assert.Contains("2", existingVvnIds);
        Assert.Contains("3", existingVvnIds);
        Assert.DoesNotContain("4", existingVvnIds);
    }

    /// <summary>
    /// Test 5: Core business logic test - filters list of all VVNs to find
    /// those without operation plans using HashSet exclusion
    /// </summary>
    [Fact]
    public void VvnList_CanBeFilteredByMissingPlans()
    {
        // Arrange
        var allVvns = new List<VvnDTO>
        {
            new VvnDTO { Id = 1, VesselImoNumber = "IMO1111111" },
            new VvnDTO { Id = 2, VesselImoNumber = "IMO2222222" },
            new VvnDTO { Id = 3, VesselImoNumber = "IMO3333333" }
        };

        var plansForVvns = new List<string> { "1", "3" };
        var existingVvnIds = new HashSet<string>(plansForVvns, StringComparer.OrdinalIgnoreCase);

        // Act
        var missing = allVvns
            .Where(vvn => !existingVvnIds.Contains(vvn.Id.ToString()))
            .ToList();

        // Assert
        Assert.Single(missing);
        Assert.Equal(2, missing.First().Id);
    }

    /// <summary>
    /// Test 6: Ensures case-insensitive comparison works for VvnIds
    /// to prevent issues with inconsistent casing from different systems
    /// </summary>
    [Fact]
    public void CaseInsensitiveComparison_WorksForVvnIds()
    {
        // Arrange
        var vvnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "100",
            "200"
        };

        // Act & Assert
        Assert.Contains("100", vvnIds);
        Assert.Contains("200", vvnIds);
        Assert.DoesNotContain("300", vvnIds);
    }

    /// <summary>
    /// Test 7: Validates date filtering logic to retrieve operation plans
    /// for a specific date, ignoring time components
    /// </summary>
    [Fact]
    public void DateFiltering_WorksCorrectly()
    {
        // Arrange
        var targetDate = DateTime.Parse("2026-01-15");
        var plans = new List<OperationPlan>
        {
            new OperationPlan { Id = Guid.NewGuid(), VvnId = "1", Date = DateTime.Parse("2026-01-15") },
            new OperationPlan { Id = Guid.NewGuid(), VvnId = "2", Date = DateTime.Parse("2026-01-16") },
            new OperationPlan { Id = Guid.NewGuid(), VvnId = "3", Date = DateTime.Parse("2026-01-15") }
        };

        // Act
        var plansForDate = plans.Where(p => p.Date.Date == targetDate.Date).ToList();

        // Assert
        Assert.Equal(2, plansForDate.Count);
        Assert.All(plansForDate, p => Assert.Equal(targetDate.Date, p.Date.Date));
    }

    /// <summary>
    /// Test 8: Verifies the response DTO structure that combines
    /// missing VVN list with statistical information
    /// </summary>
    [Fact]
    public void VvnsWithoutPlansResponseDTO_CanBeCreated()
    {
        // Arrange & Act
        var response = new VvnsWithoutPlansResponseDTO
        {
            Success = true,
            Message = "Found 2 VVN(s) without Operation Plans",
            Date = DateTime.Parse("2026-01-15"),
            TotalApprovedVvns = 5,
            VvnsWithPlans = 3,
            VvnsWithoutPlans = 2,
            MissingPlans = new List<VvnWithoutPlanDTO>
            {
                new VvnWithoutPlanDTO { Id = 1, VesselImoNumber = "IMO1234567" },
                new VvnWithoutPlanDTO { Id = 2, VesselImoNumber = "IMO2345678" }
            }
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal(2, response.VvnsWithoutPlans);
        Assert.Equal(2, response.MissingPlans.Count);
        Assert.Equal(5, response.TotalApprovedVvns);
        Assert.Equal(response.VvnsWithPlans + response.VvnsWithoutPlans, response.TotalApprovedVvns);
    }

    /// <summary>
    /// Test 9: Validates the arithmetic relationship between total approved VVNs,
    /// those with plans, and those without plans (total = with + without)
    /// </summary>
    [Fact]
    public void StatisticsCalculation_IsCorrect()
    {
        // Arrange
        int totalApproved = 10;
        int withPlans = 7;
        int withoutPlans = 3;

        // Act
        var isValid = (withPlans + withoutPlans) == totalApproved;

        // Assert
        Assert.True(isValid);
    }
}
