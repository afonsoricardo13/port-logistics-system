using System.Text;
using System.Text.Json;
using Application.DTO;
using DataModel.Repository;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using WebApi.IntegrationTests.Helpers;
using Xunit;

namespace WebApi.IntegrationTests;


// based on https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0

public class SomeIntegrationTests 
    : IClassFixture<IntegrationTestsWebApplicationFactory<Program>>
{
    private readonly IntegrationTestsWebApplicationFactory<Program> _factory;

    private readonly HttpClient _client;

    public SomeIntegrationTests(IntegrationTestsWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        // WeatherForecast não tem RequirePermission, mas não faz mal usar authenticated client
        _client = _factory.CreateAuthenticatedClient();
    }

    [Theory]
    [InlineData("/WeatherForecast")]
    public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
    {
        // Arrange

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("application/json; charset=utf-8", 
            response.Content.Headers.ContentType?.ToString());
    }
}