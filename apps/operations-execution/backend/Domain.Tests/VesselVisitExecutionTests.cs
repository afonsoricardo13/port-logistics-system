using Domain.Model;
using Xunit;

namespace Domain.Tests;

public class VesselVisitExecutionTests
{
    [Fact]
    public void VesselVisitExecution_Creation_ShouldSetProperties()
    {
        // Arrange & Act
        var vve = new VesselVisitExecution
        {
            Id = "VVE-2025-001",
            VvnId = "VVN-2025-001",
            ActualArrivalTime = DateTime.UtcNow,
            Status = VesselVisitExecutionStatus.InProgress,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("VVE-2025-001", vve.Id);
        Assert.Equal("VVN-2025-001", vve.VvnId);
        Assert.Equal(VesselVisitExecutionStatus.InProgress, vve.Status);
        Assert.NotNull(vve.ActualArrivalTime);
    }

    [Theory]
    [InlineData(VesselVisitExecutionStatus.InProgress)]
    [InlineData(VesselVisitExecutionStatus.Completed)]
    public void VesselVisitExecutionStatus_ShouldAcceptValidValues(VesselVisitExecutionStatus status)
    {
        // Arrange & Act
        var vve = new VesselVisitExecution
        {
            Status = status
        };

        // Assert
        Assert.Equal(status, vve.Status);
    }
}
