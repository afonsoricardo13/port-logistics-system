using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Proxy controller to forward scheduling requests to the Prolog server
/// Solves HTTPS/HTTP mixed content issue
/// </summary>
[ApiController]
[Route("api/scheduling")]
public class SchedulingProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SchedulingProxyController> _logger;

    public SchedulingProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SchedulingProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetSchedulingApiUrl()
    {
        // Check environment variable first, then appsettings, then default to VM
        return Environment.GetEnvironmentVariable("SCHEDULING_API_URL")
            ?? _configuration["SchedulingApiUrl"]
            ?? "http://135.225.84.188:5003";
    }

    /// <summary>
    /// Health check endpoint - proxies to Prolog server
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var schedulingUrl = GetSchedulingApiUrl();
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{schedulingUrl}/api/scheduling/health");
            
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying health check to scheduling service");
            return StatusCode(500, new { error = "Failed to reach scheduling service", detail = ex.Message });
        }
    }

    /// <summary>
    /// Generate daily schedule - proxies to Prolog server
    /// </summary>
    [HttpPost("daily")]
    public async Task<IActionResult> GenerateDailySchedule([FromBody] object request)
    {
        try
        {
            var schedulingUrl = GetSchedulingApiUrl();
            var client = _httpClientFactory.CreateClient();
            
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{schedulingUrl}/api/scheduling/daily", jsonContent);
            var content = await response.Content.ReadAsStringAsync();
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying daily schedule generation to scheduling service");
            return StatusCode(500, new { error = "Failed to generate schedule", detail = ex.Message });
        }
    }

    /// <summary>
    /// Generate schedule with algorithm selection - proxies to Prolog server
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateSchedule([FromBody] object request)
    {
        try
        {
            var schedulingUrl = GetSchedulingApiUrl();
            var client = _httpClientFactory.CreateClient();
            
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{schedulingUrl}/api/scheduling/generate", jsonContent);
            var content = await response.Content.ReadAsStringAsync();
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying schedule generation to scheduling service");
            return StatusCode(500, new { error = "Failed to generate schedule", detail = ex.Message });
        }
    }
}
