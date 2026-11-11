using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for GDPR Data Rights management (US 4.5.3).
    /// Provides endpoints for users to exercise their data rights:
    /// - Right of Access (RGPD Art. 15)
    /// - Right to Rectification (RGPD Art. 16)
    /// - Right to Erasure (RGPD Art. 17)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DataRightsController : ControllerBase
    {
        private readonly IDataRightsService _service;
        private readonly ILogger<DataRightsController> _logger;

        public DataRightsController(
            IDataRightsService service,
            ILogger<DataRightsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current user's email from the JWT token.
        /// </summary>
        private string GetUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.Identity?.Name
                ?? throw new InvalidOperationException("User email not found in token");
        }

        /// <summary>
        /// POST: api/datarights/access
        /// Creates a data access request (right to access personal data).
        /// RGPD Article 15.
        /// </summary>
        [HttpPost("access")]
        [Authorize]
        public async Task<ActionResult<DataRightsRequestDTO>> CreateAccessRequest([FromBody] CreateDataAccessRequestDTO dto)
        {
            try
            {
                var userEmail = GetUserEmail();
                _logger.LogInformation("User {Email} creating data access request", userEmail);

                var request = await _service.CreateAccessRequestAsync(userEmail, dto);

                _logger.LogInformation("Data access request {Id} created for user {Email}", request.Id, userEmail);
                return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, request);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to create data access request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data access request");
                return StatusCode(500, new { message = "An error occurred while creating the request" });
            }
        }

        /// <summary>
        /// POST: api/datarights/rectification
        /// Creates a data rectification request (right to correct personal data).
        /// RGPD Article 16.
        /// </summary>
        [HttpPost("rectification")]
        [Authorize]
        public async Task<ActionResult<DataRightsRequestDTO>> CreateRectificationRequest([FromBody] CreateDataRectificationRequestDTO dto)
        {
            try
            {
                var userEmail = GetUserEmail();
                _logger.LogInformation("User {Email} creating data rectification request", userEmail);

                var request = await _service.CreateRectificationRequestAsync(userEmail, dto);

                _logger.LogInformation("Data rectification request {Id} created for user {Email}", request.Id, userEmail);
                return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, request);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to create data rectification request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data rectification request");
                return StatusCode(500, new { message = "An error occurred while creating the request" });
            }
        }

        /// <summary>
        /// POST: api/datarights/deletion
        /// Creates a data deletion request (right to be forgotten).
        /// RGPD Article 17.
        /// </summary>
        [HttpPost("deletion")]
        [Authorize]
        public async Task<ActionResult<DataRightsRequestDTO>> CreateDeletionRequest([FromBody] CreateDataDeletionRequestDTO dto)
        {
            try
            {
                var userEmail = GetUserEmail();
                _logger.LogInformation("User {Email} creating data deletion request", userEmail);

                var request = await _service.CreateDeletionRequestAsync(userEmail, dto);

                _logger.LogInformation("Data deletion request {Id} created for user {Email}", request.Id, userEmail);
                return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, request);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to create data deletion request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data deletion request");
                return StatusCode(500, new { message = "An error occurred while creating the request" });
            }
        }

        /// <summary>
        /// GET: api/datarights/my-requests
        /// Gets all data rights requests for the current user.
        /// </summary>
        [HttpGet("my-requests")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<DataRightsRequestDTO>>> GetMyRequests()
        {
            try
            {
                var userEmail = GetUserEmail();
                _logger.LogInformation("User {Email} retrieving their data rights requests", userEmail);

                var requests = await _service.GetUserRequestsAsync(userEmail);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's data rights requests");
                return StatusCode(500, new { message = "An error occurred while retrieving requests" });
            }
        }

        /// <summary>
        /// GET: api/datarights/{id}
        /// Gets a specific data rights request by ID.
        /// Users can only view their own requests, admins can view all.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<DataRightsRequestDTO>> GetRequest(int id)
        {
            try
            {
                var userEmail = GetUserEmail();
                var isAdmin = User.IsInRole("Administrator");

                var request = await _service.GetRequestByIdAsync(id);
                if (request == null)
                {
                    return NotFound(new { message = $"Request with ID {id} not found" });
                }

                // Non-admins can only view their own requests
                if (!isAdmin && request.UserEmail != userEmail)
                {
                    return Forbid();
                }

                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data rights request {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the request" });
            }
        }

        /// <summary>
        /// GET: api/datarights/export
        /// Exports the current user's personal data (data portability).
        /// RGPD Article 20.
        /// </summary>
        [HttpGet("export")]
        [Authorize]
        public async Task<ActionResult> ExportMyData([FromQuery] string format = "json")
        {
            try
            {
                var userEmail = GetUserEmail();
                _logger.LogInformation("User {Email} exporting their data in format {Format}", userEmail, format);

                var export = await _service.ExportUserDataAsync(userEmail, format);

                if (format.ToLower() == "json")
                {
                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    var jsonContent = JsonSerializer.Serialize(export, jsonOptions);
                    var bytes = Encoding.UTF8.GetBytes(jsonContent);
                    return File(bytes, "application/json", $"my-data-export-{DateTime.UtcNow:yyyyMMdd}.json");
                }
                else
                {
                    // For PDF, return JSON structure that frontend can convert to PDF
                    return Ok(export);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to export user data");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting user data");
                return StatusCode(500, new { message = "An error occurred while exporting data" });
            }
        }

        // ==================== ADMIN ENDPOINTS ====================

        /// <summary>
        /// GET: api/datarights/admin/all
        /// Admin endpoint - gets all data rights requests.
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<DataRightsRequestDTO>>> GetAllRequests()
        {
            try
            {
                _logger.LogInformation("Admin retrieving all data rights requests");
                var requests = await _service.GetAllRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all data rights requests");
                return StatusCode(500, new { message = "An error occurred while retrieving requests" });
            }
        }

        /// <summary>
        /// GET: api/datarights/admin/pending
        /// Admin endpoint - gets pending data rights requests.
        /// </summary>
        [HttpGet("admin/pending")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<DataRightsRequestDTO>>> GetPendingRequests()
        {
            try
            {
                _logger.LogInformation("Admin retrieving pending data rights requests");
                var requests = await _service.GetPendingRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending data rights requests");
                return StatusCode(500, new { message = "An error occurred while retrieving requests" });
            }
        }

        /// <summary>
        /// PUT: api/datarights/admin/process
        /// Admin endpoint - processes a data rights request.
        /// </summary>
        [HttpPut("admin/process")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<DataRightsRequestDTO>> ProcessRequest([FromBody] ProcessDataRightsRequestDTO dto)
        {
            try
            {
                var adminEmail = GetUserEmail();
                _logger.LogInformation("Admin {Email} processing data rights request {Id}", adminEmail, dto.RequestId);

                var processed = await _service.ProcessRequestAsync(adminEmail, dto);

                _logger.LogInformation("Data rights request {Id} processed by admin {Email}, status: {Status}",
                    processed.Id, adminEmail, processed.StatusDisplay);

                return Ok(processed);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to process data rights request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data rights request");
                return StatusCode(500, new { message = "An error occurred while processing the request" });
            }
        }
    }
}
