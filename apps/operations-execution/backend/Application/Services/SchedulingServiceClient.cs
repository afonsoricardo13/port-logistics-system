using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Service to communicate with the Prolog-based Planning & Scheduling module
/// </summary>
public class SchedulingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SchedulingServiceClient> _logger;
    private readonly string _schedulingServiceUrl;

    public SchedulingServiceClient(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<SchedulingServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _schedulingServiceUrl = configuration["ExternalServices:SchedulingServiceUrl"] 
            ?? "http://localhost:5003/api/scheduling";
        
        // Configure HttpClient to accept self-signed certificates (development)
        _logger.LogInformation("Scheduling Service URL: {Url}", _schedulingServiceUrl);
    }

    /// <summary>
    /// Generate daily schedule by calling the Prolog scheduling service
    /// </summary>
    public async Task<SchedulingServiceResponseDTO> GenerateDailyScheduleAsync(
        DateTime date, 
        string algorithm = "optimal")
    {
        try
        {
            var requestBody = new
            {
                date = date.ToString("yyyy-MM-dd"),
                algorithm = algorithm
            };

            _logger.LogInformation(
                "Calling scheduling service for date {Date} with algorithm {Algorithm}", 
                date.ToString("yyyy-MM-dd"), 
                algorithm);

            var response = await _httpClient.PostAsJsonAsync(
                $"{_schedulingServiceUrl}/daily", 
                requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Scheduling service returned error. Status: {Status}, Content: {Content}",
                    response.StatusCode,
                    errorContent);
                
                return new SchedulingServiceResponseDTO
                {
                    Success = false,
                    Message = $"Scheduling service error: {response.StatusCode}",
                    Date = date
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Scheduling service raw response: {Response}", responseContent);
            
            // Parse Prolog response which has different structure
            var prologResponse = JsonSerializer.Deserialize<PrologSchedulingResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (prologResponse == null)
            {
                _logger.LogError("Failed to deserialize scheduling service response");
                return new SchedulingServiceResponseDTO
                {
                    Success = false,
                    Message = "Failed to parse scheduling service response",
                    Date = date
                };
            }

            // Convert Prolog format to our DTO format
            var result = ConvertPrologResponseToDTO(prologResponse, date);

            _logger.LogInformation(
                "Successfully received schedule for {Date}. Vessels: {Count}, Schedule items: {ScheduleCount}", 
                date.ToString("yyyy-MM-dd"), 
                result.VesselCount,
                result.Schedule?.Count ?? 0);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling scheduling service");
            return new SchedulingServiceResponseDTO
            {
                Success = false,
                Message = $"Connection error: {ex.Message}",
                Date = date
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling scheduling service");
            return new SchedulingServiceResponseDTO
            {
                Success = false,
                Message = $"Unexpected error: {ex.Message}",
                Date = date
            };
        }
    }

    /// <summary>
    /// Health check for the scheduling service
    /// </summary>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_schedulingServiceUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scheduling service health check failed");
            return false;
        }
    }

    /// <summary>
    /// Convert Prolog response format to our DTO format
    /// </summary>
    private SchedulingServiceResponseDTO ConvertPrologResponseToDTO(PrologSchedulingResponse prolog, DateTime date)
    {
        var schedule = new List<VesselScheduleDTO>();

        _logger.LogInformation("Converting Prolog response with {Count} schedule items", prolog.Schedule.Count);

        foreach (var item in prolog.Schedule)
        {
            _logger.LogInformation(
                "Processing vessel_id: {VesselId}, dock: {Dock}, crane: {Crane}, staff: {Staff}, storage: {Storage}",
                item.VesselId ?? "NULL",
                item.AssignedDock ?? "NULL",
                item.AssignedCrane ?? "NULL",
                item.AssignedStaff ?? "NULL",
                item.AssignedStorage ?? "NULL");

            var startTime = ParsePrologTime(date, item.StartTime);
            var endTime = ParsePrologTime(date, item.EndTime);
            
            var vesselSchedule = new VesselScheduleDTO
            {
                VvnId = item.VesselId ?? string.Empty,
                VesselImo = item.VesselId ?? string.Empty,
                AssignedDock = item.AssignedDock,
                StartTime = startTime,
                EndTime = endTime,
                Delay = item.Delay ?? 0,
                Operations = new List<OperationDetailDTO>(),
                Resources = new List<ResourceAssignmentDTO>()
            };
            
            _logger.LogInformation(
                "Vessel {VesselId} - Delay from Prolog: {Delay}",
                item.VesselId,
                item.Delay);

            // Add operation details
            if (vesselSchedule.StartTime.HasValue && vesselSchedule.EndTime.HasValue)
            {
                var operation = new OperationDetailDTO
                {
                    Type = "combined",
                    StartTime = vesselSchedule.StartTime.Value,
                    EndTime = vesselSchedule.EndTime.Value,
                    Duration = (int)(vesselSchedule.EndTime.Value - vesselSchedule.StartTime.Value).TotalMinutes,
                    AssignedCrane = item.AssignedCrane,
                    AssignedDock = item.AssignedDock
                };
                vesselSchedule.Operations.Add(operation);
            }

            // Add resource assignments
            if (!string.IsNullOrEmpty(item.AssignedCrane))
            {
                vesselSchedule.Resources.Add(new ResourceAssignmentDTO
                {
                    Type = "crane",
                    Id = item.AssignedCrane,
                    Name = item.AssignedCrane,
                    AssignedFrom = vesselSchedule.StartTime,
                    AssignedTo = vesselSchedule.EndTime
                });
            }

            if (!string.IsNullOrEmpty(item.AssignedStaff))
            {
                vesselSchedule.Resources.Add(new ResourceAssignmentDTO
                {
                    Type = "staff",
                    Id = item.AssignedStaff,
                    Name = item.AssignedStaff,
                    AssignedFrom = vesselSchedule.StartTime,
                    AssignedTo = vesselSchedule.EndTime
                });
            }

            if (!string.IsNullOrEmpty(item.AssignedStorage))
            {
                vesselSchedule.Resources.Add(new ResourceAssignmentDTO
                {
                    Type = "storage",
                    Id = item.AssignedStorage,
                    Name = item.AssignedStorage
                });
            }

            schedule.Add(vesselSchedule);
        }

        return new SchedulingServiceResponseDTO
        {
            Success = prolog.Success,
            Message = prolog.Success ? "Schedule generated successfully" : "Failed to generate schedule",
            Date = date,
            VesselCount = prolog.VesselCount,
            Algorithm = prolog.Algorithm ?? "unknown",
            Schedule = schedule,
            ComputationTimeMs = prolog.ComputationTimeMs,
            TotalDelay = prolog.TotalDelay
        };
    }

    /// <summary>
    /// Parse Prolog time format (HH:MM) to DateTime
    /// </summary>
    private DateTime? ParsePrologTime(DateTime date, string? timeStr)
    {
        if (string.IsNullOrEmpty(timeStr)) return null;

        try
        {
            var parts = timeStr.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
            {
                return new DateTime(date.Year, date.Month, date.Day, hours, minutes, 0);
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return null;
    }
}

/// <summary>
/// Prolog service response format
/// </summary>
internal class PrologSchedulingResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("vessel_count")]
    public int VesselCount { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("computation_time_ms")]
    public double? ComputationTimeMs { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("total_delay")]
    public double? TotalDelay { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("schedule")]
    public List<PrologScheduleItem> Schedule { get; set; } = new();
}

/// <summary>
/// Individual schedule item from Prolog
/// </summary>
internal class PrologScheduleItem
{
    [System.Text.Json.Serialization.JsonPropertyName("vessel_id")]
    public string? VesselId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("assigned_dock")]
    public string? AssignedDock { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("assigned_crane")]
    public string? AssignedCrane { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("assigned_staff")]
    public string? AssignedStaff { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("assigned_storage")]
    public string? AssignedStorage { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("start_time")]
    public string? StartTime { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("duration")]
    public string? Duration { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("delay")]
    public double? Delay { get; set; }
}
