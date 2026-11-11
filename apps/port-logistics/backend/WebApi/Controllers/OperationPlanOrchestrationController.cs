using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Application.DTO;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/planning/operation-plans")]
    public class OperationPlanOrchestrationController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly VesselVisitNotificationService _vvnService;

        public OperationPlanOrchestrationController(IHttpClientFactory httpClientFactory, IConfiguration config, VesselVisitNotificationService vvnService)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _vvnService = vvnService;
        }

        public class GenerateAndSaveRequest
        {
            public string Date { get; set; } = string.Empty; // YYYY-MM-DD
            public string Algorithm { get; set; } = "optimal"; // optimal|heuristic|multi_crane
        }

        public class SaveSummary
        {
            public bool Success { get; set; }
            public string Date { get; set; } = string.Empty;
            public string Algorithm { get; set; } = string.Empty;
            public int PlansCreated { get; set; }
            public List<string> Messages { get; set; } = new();
        }

        [HttpPost("generate-and-save")]
        public async Task<ActionResult<SaveSummary>> GenerateAndSave([FromBody] GenerateAndSaveRequest req)
        {
            var summary = new SaveSummary { Date = req.Date, Algorithm = req.Algorithm };
            var createdBy = Request.Headers["X-User-Email"].FirstOrDefault() ?? "unknown@local";

            var schedulingBase = _config["ExternalApis:Scheduling"] ?? "http://localhost:5003/api/scheduling";
            var oemBase = _config["ExternalApis:OEM"] ?? "https://localhost:7289/api";

            var http = _httpClientFactory.CreateClient();

            // 1) Call scheduling daily endpoint
            var schedulingUrl = $"{schedulingBase.TrimEnd('/')}/daily";
            var schedulingResponse = await http.PostAsJsonAsync(schedulingUrl, new { date = req.Date, algorithm = req.Algorithm });
            if (!schedulingResponse.IsSuccessStatusCode)
            {
                summary.Messages.Add($"Scheduling call failed: {(int)schedulingResponse.StatusCode} {schedulingResponse.ReasonPhrase}");
                summary.Success = false;
                return StatusCode((int)schedulingResponse.StatusCode, summary);
            }

            var schedule = await schedulingResponse.Content.ReadFromJsonAsync<dynamic>();
            if (schedule == null)
            {
                summary.Messages.Add("Scheduling returned no content");
                summary.Success = false;
                return BadRequest(summary);
            }

            // 2) Fetch Approved VVNs for the date via filter
            var date = DateTime.Parse(req.Date);
            var dayStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
            var dayEnd = dayStart.AddDays(1).AddSeconds(-1);
            var filter = new VesselVisitNotificationFilterDTO
            {
                Status = Domain.Model.VvnStatus.Approved.ToString(),
                DateFrom = dayStart,
                DateTo = dayEnd
            };
            var vvnsEnum = await _vvnService.GetFiltered(filter);
            var vvns = vvnsEnum.ToList();
            if (vvns.Count == 0)
            {
                summary.Messages.Add("No Approved VVNs for selected date");
            }

            // 3) Map schedule entries to VVNs by IMONumber and save to OEM
            int created = 0;
            var createdMsgs = new List<string>();
            try
            {
                foreach (var op in schedule.schedule)
                {
                    string vesselId = op.vessel_id;
                    var match = vvns.FirstOrDefault(v => string.Equals(v.VesselImoNumber, vesselId, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        createdMsgs.Add($"No VVN match for vessel_id {vesselId}");
                        continue;
                    }

                    var payload = new
                    {
                        VesselImoNumber = match.VesselImoNumber,
                        Date = req.Date,
                        Status = "Draft",
                        Operations = System.Text.Json.JsonSerializer.Serialize(op.operations ?? new object[]{}),
                        AssignedResources = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            dock = op.assigned_dock,
                            crane = op.assigned_crane,
                            cranes = op.assigned_cranes,
                            staff = op.assigned_staff,
                            storage = op.assigned_storage
                        }),
                        TimeWindows = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            start = op.start_time,
                            end = op.end_time
                        }),
                        CreatedAt = DateTime.UtcNow.ToString("o"),
                        CreatedBy = createdBy,
                        AlgorithmUsed = req.Algorithm
                    };

                    var reqMessage = new HttpRequestMessage(HttpMethod.Post, $"{oemBase.TrimEnd('/')}/operationplans")
                    {
                        Content = JsonContent.Create(payload)
                    };
                    // Dev auth propagation
                    reqMessage.Headers.Add("X-User-Email", createdBy);

                    var oemResp = await http.SendAsync(reqMessage);
                    if (oemResp.IsSuccessStatusCode)
                    {
                        created++;
                    }
                    else
                    {
                        createdMsgs.Add($"OEM save failed for {match.VesselImoNumber}: {(int)oemResp.StatusCode} {oemResp.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                summary.Messages.Add($"Error processing save: {ex.Message}");
            }

            summary.PlansCreated = created;
            summary.Messages.AddRange(createdMsgs);
            summary.Success = created > 0;
            return Ok(summary);
        }
    }
}
