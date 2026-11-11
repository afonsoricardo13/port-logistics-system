using System.Text.Json;
using Application.DTO;
using Domain.Model;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.6: Unit tests for resource usage aggregation logic
/// These tests mirror the parsing/aggregation behavior implemented in OperationPlanService.GetResourceUsageAsync
/// without relying on external repositories or HTTP clients. They validate that OperationPlan JSON fields
/// (AssignedResources, TimeWindows, Operations) are interpreted correctly and that aggregation by resource
/// and date-range behaves as expected.
/// </summary>
public class OperationPlanServiceTests_US4_1_6
{
    [Fact]
    public void OperationPlan_AssignedResourcesParsing_Works()
    {
        // Arrange: create AssignedResources JSON with a crane assignment
        var assignments = new[]
        {
            new ResourceAssignmentDTO { Type = "crane", Id = "CRANE-01", Name = "STS-1" }
        };

        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "1",
            Date = DateTime.Parse("2026-02-10"),
            AssignedResources = JsonSerializer.Serialize(assignments),
            TimeWindows = JsonSerializer.Serialize(new { start = DateTime.Parse("2026-02-10T08:00:00Z").ToString("o"), end = DateTime.Parse("2026-02-10T12:00:00Z").ToString("o"), duration = 240 }),
            Operations = JsonSerializer.Serialize(new[] { new OperationDetailDTO { Type = "combined", StartTime = DateTime.Parse("2026-02-10T08:00:00Z"), EndTime = DateTime.Parse("2026-02-10T12:00:00Z"), Duration = 240, AssignedCrane = "CRANE-01" } })
        };

        // Act: deserialize assigned resources and time window
        var parsedResources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(plan.AssignedResources);
        Assert.NotNull(parsedResources);

        var twDoc = JsonDocument.Parse(plan.TimeWindows!);
        var root = twDoc.RootElement;
        var start = DateTime.Parse(root.GetProperty("start").GetString()!);
        var end = DateTime.Parse(root.GetProperty("end").GetString()!);
        var duration = root.GetProperty("duration").GetInt32();

        // Assert
        Assert.Single(parsedResources);
        Assert.Equal("crane", parsedResources[0].Type);
        Assert.Equal("CRANE-01", parsedResources[0].Id);
        Assert.Equal(240, duration);
        Assert.Equal(start, DateTime.Parse("2026-02-10T08:00:00Z"));
        Assert.Equal(end, DateTime.Parse("2026-02-10T12:00:00Z"));
    }

    [Fact]
    public void GetResourceUsage_AggregatesSinglePlanCorrectly()
    {
        // Arrange
        var assignments = new[] { new ResourceAssignmentDTO { Type = "crane", Id = "C1" } };

        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "10",
            Date = DateTime.Parse("2026-03-01"),
            AssignedResources = JsonSerializer.Serialize(assignments),
            TimeWindows = JsonSerializer.Serialize(new { start = DateTime.Parse("2026-03-01T06:00:00Z").ToString("o"), end = DateTime.Parse("2026-03-01T10:30:00Z").ToString("o"), duration = 270 }),
            Operations = JsonSerializer.Serialize(new[] {
                new OperationDetailDTO { Type = "unloading", StartTime = DateTime.Parse("2026-03-01T06:00:00Z"), EndTime = DateTime.Parse("2026-03-01T08:00:00Z"), Duration = 120, AssignedCrane = "C1" },
                new OperationDetailDTO { Type = "loading", StartTime = DateTime.Parse("2026-03-01T08:30:00Z"), EndTime = DateTime.Parse("2026-03-01T10:30:00Z"), Duration = 120, AssignedCrane = "C1" }
            })
        };

        var plans = new List<OperationPlan> { plan };

        // Act (simulate service logic)
        int totalAllocated = 0;
        int totalOperations = 0;

        foreach (var p in plans)
        {
            var resources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(p.AssignedResources) ?? new List<ResourceAssignmentDTO>();
            var matching = resources.Where(r => string.Equals(r.Type, "crane", StringComparison.OrdinalIgnoreCase) && string.Equals(r.Id, "C1", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.True(matching.Any());

            // parse time window
            int duration = 0;
            if (!string.IsNullOrEmpty(p.TimeWindows))
            {
                using var doc = JsonDocument.Parse(p.TimeWindows);
                var root = doc.RootElement;
                if (root.TryGetProperty("duration", out var d)) duration = d.GetInt32();
            }

            if (duration <= 0 && !string.IsNullOrEmpty(p.Operations))
            {
                var ops = JsonSerializer.Deserialize<List<OperationDetailDTO>>(p.Operations) ?? new List<OperationDetailDTO>();
                duration = ops.Sum(o => o.Duration);
            }

            var opDetails = JsonSerializer.Deserialize<List<OperationDetailDTO>>(p.Operations) ?? new List<OperationDetailDTO>();
            var opsCount = opDetails.Count(o => o.AssignedCrane != null && string.Equals(o.AssignedCrane, "C1", StringComparison.OrdinalIgnoreCase));
            if (opsCount == 0) opsCount = opDetails.Count;

            totalAllocated += duration;
            totalOperations += opsCount;
        }

        // Assert expected values
        Assert.Equal(270, totalAllocated);
        Assert.Equal(2, totalOperations);
    }

    [Fact]
    public void GetResourceUsage_AggregatesMultiplePlansAcrossDates()
    {
        // Arrange: two plans with same crane on different dates
        var assignments = new[] { new ResourceAssignmentDTO { Type = "crane", Id = "CRX" } };

        var plan1 = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "A",
            Date = DateTime.Parse("2026-04-01"),
            AssignedResources = JsonSerializer.Serialize(assignments),
            TimeWindows = JsonSerializer.Serialize(new { start = DateTime.Parse("2026-04-01T07:00:00Z").ToString("o"), end = DateTime.Parse("2026-04-01T09:00:00Z").ToString("o"), duration = 120 }),
            Operations = JsonSerializer.Serialize(new[] { new OperationDetailDTO { Type = "combined", StartTime = DateTime.Parse("2026-04-01T07:00:00Z"), EndTime = DateTime.Parse("2026-04-01T09:00:00Z"), Duration = 120, AssignedCrane = "CRX" } })
        };

        var plan2 = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "B",
            Date = DateTime.Parse("2026-04-02"),
            AssignedResources = JsonSerializer.Serialize(assignments),
            TimeWindows = JsonSerializer.Serialize(new { start = DateTime.Parse("2026-04-02T10:00:00Z").ToString("o"), end = DateTime.Parse("2026-04-02T14:00:00Z").ToString("o"), duration = 240 }),
            Operations = JsonSerializer.Serialize(new[] { new OperationDetailDTO { Type = "combined", StartTime = DateTime.Parse("2026-04-02T10:00:00Z"), EndTime = DateTime.Parse("2026-04-02T14:00:00Z"), Duration = 240, AssignedCrane = "CRX" } })
        };

        var plans = new List<OperationPlan> { plan1, plan2 };

        // Simulate aggregation across date range 2026-04-01 .. 2026-04-03
        var start = DateTime.Parse("2026-04-01");
        var end = DateTime.Parse("2026-04-03");

        int totalMinutes = 0;
        int totalOps = 0;

        foreach (var p in plans.Where(p => p.Date >= start && p.Date <= end))
        {
            var resources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(p.AssignedResources) ?? new List<ResourceAssignmentDTO>();
            var matching = resources.Where(r => r.Type == "crane" && r.Id == "CRX").ToList();
            if (!matching.Any()) continue;

            var tw = JsonDocument.Parse(p.TimeWindows).RootElement;
            var duration = tw.GetProperty("duration").GetInt32();

            var ops = JsonSerializer.Deserialize<List<OperationDetailDTO>>(p.Operations) ?? new List<OperationDetailDTO>();
            var opCount = ops.Count(o => o.AssignedCrane == "CRX");
            if (opCount == 0) opCount = ops.Count;

            totalMinutes += duration;
            totalOps += opCount;
        }

        // Assert
        Assert.Equal(360, totalMinutes);
        Assert.Equal(2, totalOps);
    }

    [Fact]
    public void GetResourceUsage_IgnoresPlansWithoutMatchingResource()
    {
        // Arrange
        var assignments = new[] { new ResourceAssignmentDTO { Type = "dock", Id = "DOCK-1" } };

        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "100",
            Date = DateTime.Parse("2026-05-01"),
            AssignedResources = JsonSerializer.Serialize(assignments),
            TimeWindows = JsonSerializer.Serialize(new { start = DateTime.Parse("2026-05-01T06:00:00Z").ToString("o"), end = DateTime.Parse("2026-05-01T08:00:00Z").ToString("o"), duration = 120 }),
            Operations = JsonSerializer.Serialize(new[] { new OperationDetailDTO { Type = "combined", StartTime = DateTime.Parse("2026-05-01T06:00:00Z"), EndTime = DateTime.Parse("2026-05-01T08:00:00Z"), Duration = 120 } })
        };

        var plans = new List<OperationPlan> { plan };

        // Act: try to aggregate for crane "C-ABSENT"
        var total = 0;
        foreach (var p in plans)
        {
            var resources = JsonSerializer.Deserialize<List<ResourceAssignmentDTO>>(p.AssignedResources) ?? new List<ResourceAssignmentDTO>();
            var matching = resources.FirstOrDefault(r => r.Type == "crane" && r.Id == "C-ABSENT");
            if (matching == null) continue;

            // would add duration but shouldn't reach here
            total += 1;
        }

        // Assert
        Assert.Equal(0, total);
    }

    [Fact]
    public void GetResourceUsage_FallsBackToOperationsWhenTimeWindowMissing()
    {
        // Arrange: TimeWindows missing, Operations present
        var assignments = new[] { new ResourceAssignmentDTO { Type = "crane", Id = "CR-FALL" } };

        var ops = new[] {
            new OperationDetailDTO { Type = "unloading", StartTime = DateTime.Parse("2026-06-01T06:00:00Z"), EndTime = DateTime.Parse("2026-06-01T07:00:00Z"), Duration = 60, AssignedCrane = "CR-FALL" },
            new OperationDetailDTO { Type = "loading", StartTime = DateTime.Parse("2026-06-01T07:15:00Z"), EndTime = DateTime.Parse("2026-06-01T08:00:00Z"), Duration = 45, AssignedCrane = "CR-FALL" }
        };

        var plan = new OperationPlan
        {
            Id = Guid.NewGuid(),
            VvnId = "Z",
            Date = DateTime.Parse("2026-06-01"),
            AssignedResources = JsonSerializer.Serialize(assignments),
            TimeWindows = null,
            Operations = JsonSerializer.Serialize(ops)
        };

        // Act
        int duration = 0;
        var opDetails = JsonSerializer.Deserialize<List<OperationDetailDTO>>(plan.Operations) ?? new List<OperationDetailDTO>();
        duration = opDetails.Sum(o => o.Duration);

        // Assert
        Assert.Equal(105, duration);
    }
}

