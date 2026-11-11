using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Controller for Privacy Policy management (US 4.5.1).
    /// Provides both public endpoints (current policy) and admin endpoints (create, history).
    /// Implements GDPR compliance requirements (RGPD Articles 13-14).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PrivacyPolicyController : ControllerBase
    {
        private readonly IPrivacyPolicyService _service;
        private readonly ILogger<PrivacyPolicyController> _logger;

        public PrivacyPolicyController(
            IPrivacyPolicyService service, 
            ILogger<PrivacyPolicyController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/privacypolicy/current
        /// Public endpoint - gets currently active privacy policy.
        /// Accessible without authentication (required for GDPR transparency).
        /// </summary>
        [HttpGet("current")]
        [AllowAnonymous]
        public async Task<ActionResult<PrivacyPolicyDto>> GetCurrent()
        {
            _logger.LogInformation("Public request for current privacy policy");

            var policy = await _service.GetCurrentPolicyAsync();

            if (policy == null)
            {
                _logger.LogWarning("No active privacy policy found");
                return NotFound(new { message = "No active privacy policy found" });
            }

            return Ok(policy);
        }

        /// <summary>
        /// GET: api/privacypolicy/version/{version}
        /// Gets a specific privacy policy version by version number.
        /// Accessible to authenticated users to view historical policies.
        /// </summary>
        [HttpGet("version/{version}")]
        [Authorize]
        public async Task<ActionResult<PrivacyPolicyDto>> GetByVersion(int version)
        {
            _logger.LogInformation("Request for privacy policy version {Version}", version);

            var policy = await _service.GetPolicyByVersionAsync(version);

            if (policy == null)
            {
                _logger.LogWarning("Privacy policy version {Version} not found", version);
                return NotFound(new { message = $"Privacy policy version {version} not found" });
            }

            return Ok(policy);
        }

        /// <summary>
        /// GET: api/privacypolicy/history
        /// Admin endpoint - gets complete history of all policy versions.
        /// Requires admin role.
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<PrivacyPolicyHistoryItemDto>>> GetHistory()
        {
            _logger.LogInformation("Admin request for privacy policy history");

            var history = await _service.GetPolicyHistoryAsync();

            return Ok(history);
        }

        /// <summary>
        /// POST: api/privacypolicy
        /// Admin endpoint - creates a new privacy policy version.
        /// Requires admin role for GDPR accountability (RGPD Art 5.2).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<PrivacyPolicyDto>> Create([FromBody] CreatePrivacyPolicyDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid privacy policy creation request");
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.Email)?.Value 
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value 
                ?? User.Identity?.Name 
                ?? "Unknown";

            _logger.LogInformation("Admin {UserId} creating new privacy policy version", userId);

            try
            {
                var created = await _service.CreateNewVersionAsync(dto, userId);

                _logger.LogInformation("Privacy policy version {Version} created successfully by {UserId}", 
                    created.Version, userId);

                return CreatedAtAction(
                    nameof(GetByVersion), 
                    new { version = created.Version }, 
                    created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating privacy policy by {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while creating the privacy policy" });
            }
        }

        /// <summary>
        /// PUT: api/privacypolicy/{id}/activate
        /// Admin endpoint - activates a specific privacy policy version.
        /// Deactivates all other versions (only one active at a time).
        /// Requires admin role.
        /// </summary>
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> Activate(int id)
        {
            var userId = User.FindFirst(ClaimTypes.Email)?.Value 
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value 
                ?? User.Identity?.Name 
                ?? "Unknown";

            _logger.LogInformation("Admin {UserId} activating privacy policy ID {Id}", userId, id);

            try
            {
                await _service.ActivatePolicyAsync(id);

                _logger.LogInformation("Privacy policy ID {Id} activated by {UserId}", id, userId);

                return Ok(new { message = $"Privacy policy {id} activated successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to activate privacy policy ID {Id}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating privacy policy ID {Id}", id);
                return StatusCode(500, new { message = "An error occurred while activating the privacy policy" });
            }
        }

        /// <summary>
        /// POST: api/privacypolicy/{id}/notify
        /// Admin endpoint - triggers user notifications about policy change.
        /// Implements RGPD Art 13(3) notification requirements.
        /// Requires admin role.
        /// </summary>
        [HttpPost("{id}/notify")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> NotifyUsers(int id)
        {
            var userId = User.FindFirst(ClaimTypes.Email)?.Value 
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst("sub")?.Value 
                ?? User.Identity?.Name 
                ?? "Unknown";

            _logger.LogInformation("Admin {UserId} triggering notifications for privacy policy ID {Id}", userId, id);

            try
            {
                await _service.NotifyUsersAboutPolicyChangeAsync(id);

                _logger.LogInformation("Privacy policy notification process initiated for ID {Id} by {UserId}", id, userId);

                return Ok(new { message = "User notification process started successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to notify users about privacy policy ID {Id}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying users about privacy policy ID {Id}", id);
                return StatusCode(500, new { message = "An error occurred while notifying users" });
            }
        }
    }
}
