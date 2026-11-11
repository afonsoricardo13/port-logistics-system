using System.Net;
using System.Net.Http.Json;
using Application.DTO;
using Domain.Model;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.6: Integration tests for Resource Usage endpoint
/// These tests spin up the WebApi using an in-memory SQLite database, seed OperationPlan entries,
/// and call the /api/OperationPlans/resource-usage endpoint to validate aggregation behavior.
/// </summary>
public class OperationPlansControllerTests_US4_1_6 : IClassFixture<IntegrationTestsWebApplicationFactory<Program>>
{
    private readonly IntegrationTestsWebApplicationFactory<Program> _factory;

    public OperationPlansControllerTests_US4_1_6(IntegrationTestsWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResourceUsageEndpoint_ReturnsAggregatedTotals_ForExistingResource()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Seed OperationPlans into the test database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataModel.OEMDbContext>();

            var plan1 = new OperationPlan
            {
                Id = Guid.NewGuid(),
                VvnId = "VVN-1",
                Date = DateTime.Parse("2026-07-01"),
                AssignedResources = System.Text.Json.JsonSerializer.Serialize(new[] { new ResourceAssignmentDTO { Type = "crane", Id = "CR-1" } }),
                TimeWindows = System.Text.Json.JsonSerializer.Serialize(new { start = DateTime.Parse("2026-07-01T08:00:00Z").ToString("o"), end = DateTime.Parse("2026-07-01T10:00:00Z").ToString("o"), duration = 120 }),
                Operations = System.Text.Json.JsonSerializer.Serialize(new[] { new OperationDetailDTO { Type = "combined", StartTime = DateTime.Parse("2026-07-01T08:00:00Z"), EndTime = DateTime.Parse("2026-07-01T10:00:00Z"), Duration = 120, AssignedCrane = "CR-1" } }),
                CreatedBy = "seed@test",
                CreatedAt = DateTime.UtcNow,
                AlgorithmUsed = "heuristic"
            };

            var plan2 = new OperationPlan
            {
                Id = Guid.NewGuid(),
                VvnId = "VVN-2",
                Date = DateTime.Parse("2026-07-02"),
                AssignedResources = System.Text.Json.JsonSerializer.Serialize(new[] { new ResourceAssignmentDTO { Type = "crane", Id = "CR-1" } }),
                TimeWindows = System.Text.Json.JsonSerializer.Serialize(new { start = DateTime.Parse("2026-07-02T09:00:00Z").ToString("o"), end = DateTime.Parse("2026-07-02T13:00:00Z").ToString("o"), duration = 240 }),
                Operations = System.Text.Json.JsonSerializer.Serialize(new[] { new OperationDetailDTO { Type = "combined", StartTime = DateTime.Parse("2026-07-02T09:00:00Z"), EndTime = DateTime.Parse("2026-07-02T13:00:00Z"), Duration = 240, AssignedCrane = "CR-1" } }),
                CreatedBy = "seed@test",
                CreatedAt = DateTime.UtcNow,
                AlgorithmUsed = "heuristic"
            };

            db.OperationPlans.Add(plan1);
            db.OperationPlans.Add(plan2);
            await db.SaveChangesAsync();
        }

        // Act: call the endpoint for CR-1 across date range that includes both plans
        var start = "2026-07-01";
        var end = "2026-07-03";
        var response = await client.GetAsync($"/api/OperationPlans/resource-usage?resourceType=crane&resourceId=CR-1&startDate={start}&endDate={end}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ResourceUsageResultDTO>();
        Assert.NotNull(result);
        Assert.Equal("crane", result.ResourceType);
        Assert.Equal("CR-1", result.ResourceId);
        Assert.Equal(360, result.TotalAllocatedMinutes); // 120 + 240
        Assert.Equal(2, result.TotalOperations);
        Assert.Equal(2, result.Plans.Count);
    }

    [Fact]
    public async Task ResourceUsageEndpoint_ReturnsEmptyResult_WhenResourceNotAssigned()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Seed a plan that uses a different resource
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataModel.OEMDbContext>();

            var plan = new OperationPlan
            {
                Id = Guid.NewGuid(),
                VvnId = "VVN-3",
                Date = DateTime.Parse("2026-08-01"),
                AssignedResources = System.Text.Json.JsonSerializer.Serialize(new[] { new ResourceAssignmentDTO { Type = "dock", Id = "DOCK-X" } }),
                TimeWindows = System.Text.Json.JsonSerializer.Serialize(new { start = DateTime.Parse("2026-08-01T06:00:00Z").ToString("o"), end = DateTime.Parse("2026-08-01T08:00:00Z").ToString("o"), duration = 120 }),
                Operations = System.Text.Json.JsonSerializer.Serialize(new[] { new OperationDetailDTO { Type = "combined", StartTime = DateTime.Parse("2026-08-01T06:00:00Z"), EndTime = DateTime.Parse("2026-08-01T08:00:00Z"), Duration = 120 } }),
                CreatedBy = "seed@test",
                CreatedAt = DateTime.UtcNow,
                AlgorithmUsed = "heuristic"
            };

            db.OperationPlans.Add(plan);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync($"/api/OperationPlans/resource-usage?resourceType=crane&resourceId=CR-ABSENT&startDate=2026-08-01&endDate=2026-08-02");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResourceUsageResultDTO>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalAllocatedMinutes);
        Assert.Equal(0, result.TotalOperations);
        Assert.Empty(result.Plans);
    }
}

