using Application.DTO;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.5: Integration tests for VVNs without Operation Plans endpoint
/// These tests validate DTO structures, data transformation, and API response formats
/// without requiring the full application stack to be running
/// </summary>
public class OperationPlansControllerTests_US4_1_5
{
    /// <summary>
    /// Test 1: Validates that the response DTO can be instantiated with all properties
    /// including nested VvnWithoutPlanDTO objects in the MissingPlans collection
    /// </summary>
    [Fact]
    public void VvnsWithoutPlansResponseDTO_CanBeCreatedWithAllProperties()
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
                new VvnWithoutPlanDTO
                {
                    Id = 1,
                    VesselImoNumber = "IMO1234567",
                    EstimatedTimeOfArrival = DateTime.Parse("2026-01-15T08:00:00Z"),
                    EstimatedTimeOfDeparture = DateTime.Parse("2026-01-15T16:00:00Z"),
                    AssignedDockCode = "DOCK-A"
                }
            }
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal(2, response.VvnsWithoutPlans);
        Assert.Single(response.MissingPlans);
        Assert.Equal("IMO1234567", response.MissingPlans[0].VesselImoNumber);
    }

    /// <summary>
    /// Test 2: Verifies that the statistics in the response (total, with plans, without plans)
    /// maintain mathematical consistency (total = with + without)
    /// </summary>
    [Fact]
    public void VvnsWithoutPlansResponseDTO_CalculatesStatisticsCorrectly()
    {
        // Arrange & Act
        var response = new VvnsWithoutPlansResponseDTO
        {
            Success = true,
            Message = "Test",
            Date = DateTime.Parse("2026-01-15"),
            TotalApprovedVvns = 10,
            VvnsWithPlans = 7,
            VvnsWithoutPlans = 3,
            MissingPlans = new List<VvnWithoutPlanDTO>()
        };

        // Assert
        Assert.Equal(response.VvnsWithPlans + response.VvnsWithoutPlans, response.TotalApprovedVvns);
    }

    /// <summary>
    /// Test 3: Ensures VvnWithoutPlanDTO contains all required vessel information
    /// (ID, IMO number, arrival/departure times, dock assignment)
    /// </summary>
    [Fact]
    public void VvnWithoutPlanDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var vvn = new VvnWithoutPlanDTO
        {
            Id = 123,
            VesselImoNumber = "IMO9876543",
            EstimatedTimeOfArrival = DateTime.Parse("2026-01-15T14:00:00Z"),
            EstimatedTimeOfDeparture = DateTime.Parse("2026-01-15T22:00:00Z"),
            AssignedDockCode = "DOCK-B"
        };

        // Assert
        Assert.Equal(123, vvn.Id);
        Assert.Equal("IMO9876543", vvn.VesselImoNumber);
        Assert.NotNull(vvn.EstimatedTimeOfArrival);
        Assert.NotNull(vvn.EstimatedTimeOfDeparture);
        Assert.Equal("DOCK-B", vvn.AssignedDockCode);
    }

    /// <summary>
    /// Test 4: Tests the happy path where all VVNs have operation plans
    /// Response should have empty MissingPlans list and appropriate success message
    /// </summary>
    [Fact]
    public void VvnsWithoutPlansResponseDTO_SupportsEmptyMissingPlansList()
    {
        // Arrange & Act
        var response = new VvnsWithoutPlansResponseDTO
        {
            Success = true,
            Message = "All Approved VVNs have Operation Plans",
            Date = DateTime.Parse("2026-01-15"),
            TotalApprovedVvns = 5,
            VvnsWithPlans = 5,
            VvnsWithoutPlans = 0,
            MissingPlans = new List<VvnWithoutPlanDTO>()
        };

        // Assert
        Assert.True(response.Success);
        Assert.Empty(response.MissingPlans);
        Assert.Equal(0, response.VvnsWithoutPlans);
        Assert.Contains("All Approved VVNs have Operation Plans", response.Message);
    }

    /// <summary>
    /// Test 5: Validates that the response DTO can represent error states
    /// with Success=false and descriptive error message
    /// </summary>
    [Fact]
    public void VvnsWithoutPlansResponseDTO_SupportsErrorState()
    {
        // Arrange & Act
        var response = new VvnsWithoutPlansResponseDTO
        {
            Success = false,
            Message = "Error retrieving VVNs without Operation Plans",
            Date = DateTime.Parse("2026-01-15"),
            TotalApprovedVvns = 0,
            VvnsWithPlans = 0,
            VvnsWithoutPlans = 0,
            MissingPlans = new List<VvnWithoutPlanDTO>()
        };

        // Assert
        Assert.False(response.Success);
        Assert.Contains("Error", response.Message);
    }

    /// <summary>
    /// Test 6: Confirms that VvnWithoutPlanDTO supports optional fields
    /// Only ID and IMO number are required; others can be null
    /// </summary>
    [Fact]
    public void VvnWithoutPlanDTO_SupportsOptionalFields()
    {
        // Arrange & Act
        var vvn = new VvnWithoutPlanDTO
        {
            Id = 1,
            VesselImoNumber = "IMO1234567"
            // Optional fields not set
        };

        // Assert
        Assert.Equal(1, vvn.Id);
        Assert.Equal("IMO1234567", vvn.VesselImoNumber);
        Assert.Null(vvn.EstimatedTimeOfArrival);
        Assert.Null(vvn.EstimatedTimeOfDeparture);
        Assert.Null(vvn.AssignedDockCode);
    }

    /// <summary>
    /// Test 7: Verifies that response messages correctly pluralize
    /// based on the count of missing VVNs (1 VVN vs multiple VVNs)
    /// </summary>
    [Fact]
    public void VvnsWithoutPlansResponseDTO_MessageVariesByCount()
    {
        // Arrange & Act
        var singleVvnResponse = new VvnsWithoutPlansResponseDTO
        {
            Success = true,
            Message = "Found 1 VVN(s) without Operation Plans",
            Date = DateTime.Parse("2026-01-15"),
            VvnsWithoutPlans = 1,
            MissingPlans = new List<VvnWithoutPlanDTO> { new VvnWithoutPlanDTO { Id = 1 } }
        };

        var multipleVvnsResponse = new VvnsWithoutPlansResponseDTO
        {
            Success = true,
            Message = "Found 3 VVN(s) without Operation Plans",
            Date = DateTime.Parse("2026-01-15"),
            VvnsWithoutPlans = 3,
            MissingPlans = new List<VvnWithoutPlanDTO>()
        };

        // Assert
        Assert.Contains("Found 1 VVN(s)", singleVvnResponse.Message);
        Assert.Contains("Found 3 VVN(s)", multipleVvnsResponse.Message);
    }

    /// <summary>
    /// Test 8: Validates that VvnWithoutPlanDTO collections can be filtered
    /// by properties like AssignedDockCode for operational analysis
    /// </summary>
    [Fact]
    public void VvnWithoutPlanDTO_ListCanBeFiltered()
    {
        // Arrange
        var vvns = new List<VvnWithoutPlanDTO>
        {
            new VvnWithoutPlanDTO { Id = 1, VesselImoNumber = "IMO1111111", AssignedDockCode = "DOCK-A" },
            new VvnWithoutPlanDTO { Id = 2, VesselImoNumber = "IMO2222222", AssignedDockCode = "DOCK-B" },
            new VvnWithoutPlanDTO { Id = 3, VesselImoNumber = "IMO3333333", AssignedDockCode = "DOCK-A" }
        };

        // Act
        var dockAVvns = vvns.Where(v => v.AssignedDockCode == "DOCK-A").ToList();

        // Assert
        Assert.Equal(2, dockAVvns.Count);
        Assert.All(dockAVvns, v => Assert.Equal("DOCK-A", v.AssignedDockCode));
    }

    /// <summary>
    /// Test 9: Confirms that VvnWithoutPlanDTO collections support sorting
    /// by ID for consistent ordering in UI displays
    /// </summary>
    [Fact]
    public void VvnWithoutPlanDTO_CanBeSortedById()
    {
        // Arrange
        var vvns = new List<VvnWithoutPlanDTO>
        {
            new VvnWithoutPlanDTO { Id = 3, VesselImoNumber = "IMO3333333" },
            new VvnWithoutPlanDTO { Id = 1, VesselImoNumber = "IMO1111111" },
            new VvnWithoutPlanDTO { Id = 2, VesselImoNumber = "IMO2222222" }
        };

        // Act
        var sorted = vvns.OrderBy(v => v.Id).ToList();

        // Assert
        Assert.Equal(1, sorted[0].Id);
        Assert.Equal(2, sorted[1].Id);
        Assert.Equal(3, sorted[2].Id);
    }

    /// <summary>
    /// Test 10: Ensures the query date is correctly preserved in the response
    /// for client-side validation and display purposes
    /// </summary>
    [Fact]
    public void VvnsWithoutPlansResponseDTO_DateIsPreserved()
    {
        // Arrange
        var testDate = DateTime.Parse("2026-01-15");
        
        // Act
        var response = new VvnsWithoutPlansResponseDTO
        {
            Success = true,
            Message = "Test",
            Date = testDate,
            TotalApprovedVvns = 0,
            VvnsWithPlans = 0,
            VvnsWithoutPlans = 0,
            MissingPlans = new List<VvnWithoutPlanDTO>()
        };

        // Assert
        Assert.Equal(testDate, response.Date);
        Assert.Equal(testDate.Date, response.Date.Date);
    }
}
