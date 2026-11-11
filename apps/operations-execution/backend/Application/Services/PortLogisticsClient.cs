using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// HTTP client to communicate with Port Logistics module to validate VVNs
/// </summary>
public class PortLogisticsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PortLogisticsClient> _logger;

    public PortLogisticsClient(HttpClient httpClient, ILogger<PortLogisticsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get VVN by ID and validate status is Approved
    /// </summary>
    public async Task<VvnValidationResult> ValidateVvnAsync(string vvnId)
    {
        try
        {
            _logger.LogInformation("Validating VVN {VvnId} in Port Logistics", vvnId);

            var response = await _httpClient.GetAsync($"/api/vessel-visits/notifications/{vvnId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VVN {VvnId} not found or error: {StatusCode}", vvnId, response.StatusCode);
                return new VvnValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"VVN {vvnId} not found in Port Logistics"
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var vvn = JsonSerializer.Deserialize<VvnDTO>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (vvn == null)
            {
                return new VvnValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Failed to parse VVN {vvnId}"
                };
            }

            // Fetch vessel name by IMO
            if (!string.IsNullOrEmpty(vvn.VesselImoNumber))
            {
                try
                {
                    var vesselResponse = await _httpClient.GetAsync($"/api/vessels/imo/{vvn.VesselImoNumber}");
                    if (vesselResponse.IsSuccessStatusCode)
                    {
                        var vesselJson = await vesselResponse.Content.ReadAsStringAsync();
                        var vessel = JsonSerializer.Deserialize<VesselDTO>(vesselJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (vessel != null && !string.IsNullOrEmpty(vessel.Name))
                        {
                            vvn.VesselName = vessel.Name;
                            _logger.LogInformation("Fetched vessel name '{VesselName}' for IMO {Imo}", vessel.Name, vvn.VesselImoNumber);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not fetch vessel name for IMO {Imo}: {StatusCode}", vvn.VesselImoNumber, vesselResponse.StatusCode);
                    }
                }
                catch (Exception vesselEx)
                {
                    _logger.LogWarning(vesselEx, "Error fetching vessel name for IMO {Imo}", vvn.VesselImoNumber);
                }
            }

            // Check if status is Approved (value 2 in VvnStatus enum)
            if (vvn.Status != "Approved")
            {
                _logger.LogWarning(
                    "VVN {VvnId} has status {Status}, only Approved VVNs can be scheduled",
                    vvnId,
                    vvn.Status);

                return new VvnValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"VVN {vvnId} has status '{vvn.Status}'. Only Approved VVNs can be scheduled."
                };
            }

            _logger.LogInformation("VVN {VvnId} is valid (Status: Approved)", vvnId);

            return new VvnValidationResult
            {
                IsValid = true,
                VesselImo = vvn.VesselImoNumber,
                AssignedDockCode = vvn.AssignedDockCode,
                VvnData = vvn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VVN {VvnId}", vvnId);
            return new VvnValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Error validating VVN {vvnId}: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get all approved VVNs for a specific date
    /// </summary>
    public async Task<List<VvnDTO>> GetApprovedVvnsForDateAsync(DateTime date)
    {
        try
        {
            _logger.LogInformation("Getting approved VVNs for date {Date}", date.ToString("yyyy-MM-dd"));
            _logger.LogInformation("HttpClient BaseAddress: {BaseAddress}", _httpClient.BaseAddress);

            // Get ALL Approved VVNs first, then filter by date in code
            // This avoids timezone/time comparison issues
            var url = $"/api/vessel-visits/notifications?status=Approved";
            _logger.LogInformation("Calling Port Logistics: {BaseAddress}{Url}", _httpClient.BaseAddress, url);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Failed to get VVNs: {StatusCode}, Content: {Content}", 
                    response.StatusCode, 
                    errorContent);
                return new List<VvnDTO>();
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Port Logistics response (first 500 chars): {Json}", 
                json.Length > 500 ? json.Substring(0, 500) + "..." : json);

            var allVvns = JsonSerializer.Deserialize<List<VvnDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<VvnDTO>();

            _logger.LogInformation(
                "Received {Count} total Approved VVNs from Port Logistics", 
                allVvns.Count);

            // Filter by date (only the date part, ignore time)
            var targetDate = date.Date;
            var vvnsForDate = allVvns.Where(v => 
            {
                // Check if ETA date matches (ignore time component)
                var etaMatches = v.EstimatedTimeOfArrival.HasValue && 
                                v.EstimatedTimeOfArrival.Value.Date == targetDate;
                
                // Double-check status is Approved
                var isApproved = v.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                
                if (etaMatches)
                {
                    _logger.LogInformation(
                        "VVN {Id} (IMO: {Imo}) - ETA: {Eta}, Status: {Status}, Matches: {Match}",
                        v.Id,
                        v.VesselImoNumber,
                        v.EstimatedTimeOfArrival?.ToString("yyyy-MM-dd HH:mm"),
                        v.Status,
                        isApproved ? "YES" : "NO (wrong status)");
                }
                
                return etaMatches && isApproved;
            }).ToList();

            _logger.LogInformation(
                "Found {Count} Approved VVNs for date {Date}: {VvnIds}", 
                vvnsForDate.Count, 
                date.ToString("yyyy-MM-dd"),
                string.Join(", ", vvnsForDate.Select(v => $"#{v.Id} IMO:{v.VesselImoNumber}")));

            return vvnsForDate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approved VVNs for date {Date}", date);
            return new List<VvnDTO>();
        }
    }

    /// <summary>
    /// Get crane capacity (containers per hour) by crane code
    /// </summary>
    public async Task<int> GetCraneCapacityAsync(string craneCode)
    {
        try
        {
            _logger.LogInformation("Getting crane capacity for {CraneCode}", craneCode);

            var response = await _httpClient.GetAsync($"/api/physical-resources/{craneCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Crane {CraneCode} not found, using default capacity 30", craneCode);
                return 30; // Default: 30 containers/hour
            }

            var json = await response.Content.ReadAsStringAsync();
            var crane = JsonSerializer.Deserialize<PhysicalResourceDTO>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (crane == null || crane.Capacity <= 0)
            {
                _logger.LogWarning("Invalid crane capacity, using default 30");
                return 30;
            }

            _logger.LogInformation("Crane {CraneCode} capacity: {Capacity} containers/hour", craneCode, crane.Capacity);
            return crane.Capacity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting crane capacity for {CraneCode}, using default 30", craneCode);
            return 30; // Default fallback
        }
    }

    /// <summary>
    /// US 4.1.4: Get physical resource by code/ID for validation
    /// </summary>
    public async Task<PhysicalResourceDetailDTO?> GetPhysicalResourceAsync(string resourceId)
    {
        try
        {
            _logger.LogInformation("Getting physical resource {ResourceId} from Port Logistics", resourceId);
            
            var response = await _httpClient.GetAsync($"/api/PhysicalResources/{resourceId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Physical resource {ResourceId} not found: {StatusCode}", resourceId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var resource = JsonSerializer.Deserialize<PhysicalResourceDetailDTO>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting physical resource {ResourceId}", resourceId);
            return null;
        }
    }

    /// <summary>
    /// US 4.1.4: Get operating staff by mecanographic number for validation
    /// </summary>
    public async Task<OperatingStaffDTO?> GetOperatingStaffAsync(string mecanographicNumber)
    {
        try
        {
            _logger.LogInformation("Getting operating staff {MecanographicNumber} from Port Logistics", mecanographicNumber);
            
            var response = await _httpClient.GetAsync($"/api/OperatingStaff/mecanographic/{mecanographicNumber}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Operating staff {MecanographicNumber} not found: {StatusCode}", mecanographicNumber, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var staff = JsonSerializer.Deserialize<OperatingStaffDTO>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return staff;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting operating staff {MecanographicNumber}", mecanographicNumber);
            return null;
        }
    }
}

/// <summary>
/// DTO for VVN from Port Logistics
/// </summary>
public class VvnDTO
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("vesselImoNumber")]
    public string VesselImoNumber { get; set; } = string.Empty;

    [JsonPropertyName("vesselName")]
    public string? VesselName { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("assignedDockCode")]
    public string? AssignedDockCode { get; set; }

    [JsonPropertyName("estimatedTimeOfArrival")]
    public DateTime? EstimatedTimeOfArrival { get; set; }

    [JsonPropertyName("estimatedTimeOfDeparture")]
    public DateTime? EstimatedTimeOfDeparture { get; set; }

    [JsonPropertyName("loadingContainerCount")]
    public int LoadingContainerCount { get; set; }

    [JsonPropertyName("totalUnloadingContainers")]
    public int TotalUnloadingContainers { get; set; }
}

/// <summary>
/// DTO for Vessel from Port Logistics
/// </summary>
public class VesselDTO
{
    [JsonPropertyName("imoNumber")]
    public string ImoNumber { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("vesselTypeCode")]
    public string? VesselTypeCode { get; set; }

    [JsonPropertyName("operatorName")]
    public string? OperatorName { get; set; }
}

/// <summary>
/// DTO for Physical Resource (Crane) from Port Logistics
/// </summary>
public class PhysicalResourceDTO
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }
}

/// <summary>
/// Result of VVN validation
/// </summary>
public class VvnValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? VesselImo { get; set; }
    public string? AssignedDockCode { get; set; }
    public VvnDTO? VvnData { get; set; }
}

/// <summary>
/// DTO for Physical Resource details (US 4.1.4)
/// </summary>
public class PhysicalResourceDetailDTO
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }
}

/// <summary>
/// DTO for Operating Staff (US 4.1.4)
/// </summary>
public class OperatingStaffDTO
{
    [JsonPropertyName("mecanographicNumber")]
    public string MecanographicNumber { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("operationalWindow")]
    public string OperationalWindow { get; set; } = string.Empty;

    [JsonPropertyName("qualifications")]
    public List<string>? Qualifications { get; set; }
}
