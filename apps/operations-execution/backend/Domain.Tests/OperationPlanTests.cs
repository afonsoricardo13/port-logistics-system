using Domain.Model;
using Xunit;

namespace Domain.Tests;

public class OperationPlanTests
{
    [Fact]
    public void OperationPlan_Creation_ShouldSetProperties()
    {
        // Arrange & Act
        var operationPlan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "VVN-2025-001",
            Date = DateTime.UtcNow.Date,
            Operations = "[]",
            AssignedResources = "[]",
            TimeWindows = "[]",
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            AlgorithmUsed = "optimal"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, operationPlan.Id);
        Assert.Equal("VVN-2025-001", operationPlan.VvnId);
        Assert.Equal("optimal", operationPlan.AlgorithmUsed);
    }

    [Fact]
    public void OperationPlan_Update_ShouldSetUpdateFields()
    {
        // Arrange
        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "VVN-2025-001",
            Date = DateTime.Today,
            Operations = "[]",
            AssignedResources = "[]",
            TimeWindows = "{}",
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            AlgorithmUsed = "optimal"
        };

        // Act
        plan.UpdatedBy = "operator@port.com";
        plan.UpdatedAt = DateTime.UtcNow;

        // Assert
        Assert.NotNull(plan.UpdatedBy);
        Assert.NotNull(plan.UpdatedAt);
    }

    [Fact]
    public void OperationPlan_WithChangeLogs_ShouldStoreJson()
    {
        // Arrange
        var changeLogJson = "[{\"timestamp\":\"2025-12-17T00:00:00Z\",\"author\":\"operator@port.com\",\"reason\":\"Resource adjustment\",\"changes\":[\"Updated crane assignment\"]}]";

        // Act
        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "VVN-2025-001",
            Date = DateTime.Today,
            Operations = "[]",
            AssignedResources = "[]",
            TimeWindows = "{}",
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            AlgorithmUsed = "optimal",
            ChangeLogs = changeLogJson,
            UpdatedBy = "operator@port.com",
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(plan.ChangeLogs);
        Assert.Contains("operator@port.com", plan.ChangeLogs);
        Assert.Contains("Resource adjustment", plan.ChangeLogs);
        Assert.Equal("operator@port.com", plan.UpdatedBy);
        Assert.NotNull(plan.UpdatedAt);
    }
}
