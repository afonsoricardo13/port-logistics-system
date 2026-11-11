using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain.Model;
using System.Security.Claims;
using WebApi.Authorization;
using Domain.IRepository;

namespace WebApi.Controllers
{
    [Route("api/vessel-visits/notifications")]
    [ApiController]
    [RequirePermission("vessel-visit-notifications")]
    public class VesselVisitNotificationsController : ControllerBase
    {
        private readonly VesselVisitNotificationService _service;
        private readonly IRepresentativeRepository _representativeRepo;
        private readonly ILogger<VesselVisitNotificationsController> _logger;
        private readonly List<string> _errors = new();

        public VesselVisitNotificationsController(
            VesselVisitNotificationService service,
            IRepresentativeRepository representativeRepo,
            ILogger<VesselVisitNotificationsController> logger)
        {
            _service = service;
            _representativeRepo = representativeRepo;
            _logger = logger;
        }

        // POST: api/vessel-visits/notifications
        // US 2.2.8: Representative creates/submits VVN
        [HttpPost]
        public async Task<ActionResult<VesselVisitNotificationDTO>> Create(VesselVisitNotificationDTO dto)
        {
            // Get authenticated user's email
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Create VVN: No email claim found");
                return Unauthorized("User email not found in authentication claims");
            }

            // Find representative by email
            var representative = await _representativeRepo.GetByEmailAsync(email);
            if (representative == null)
            {
                _logger.LogWarning("Create VVN: No representative found for email {Email}", email);
                return StatusCode(403, $"No representative found for email {email}");
            }

            if (!representative.IsActive)
            {
                _logger.LogWarning("Create VVN: Representative {Id} is not active", representative.Id);
                return StatusCode(403, "Representative is not active");
            }

            _logger.LogInformation("Creating Vessel Visit Notification for vessel {Imo} by representative {RepId}", 
                dto.VesselImoNumber, representative.Id);
            
            var created = await _service.Create(dto, _errors, representative.Id);
            if (created == null)
            {
                return BadRequest(_errors);
            }
            
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // GET: api/vessel-visits/notifications/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<VesselVisitNotificationDTO>> GetById(long id)
        {
            var dto = await _service.GetById(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // PATCH: api/vessel-visits/notifications/{id}/decision?officerId=...
        // Unified endpoint to approve or reject a vessel visit notification
        public class VvnDecisionDTO
        {
            public string Status { get; set; } = string.Empty; // Approved | Rejected
            public string? AssignedDockCode { get; set; }       // required when Approved
            public string? Reason { get; set; }                 // required when Rejected
        }

        [HttpPatch("{id:long}/decision")]
        public async Task<ActionResult<VesselVisitNotificationDTO>> UpdateDecision(long id, [FromQuery] string officerId, [FromBody] VvnDecisionDTO dto)
        {
            if (dto == null) return BadRequest("Body is required");

            var status = dto.Status?.Trim();
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("Status is required (Approved | Rejected)");

            if (string.Equals(status, VvnStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(dto.AssignedDockCode))
                    return BadRequest("AssignedDockCode is required when approving");

                _logger.LogInformation("Approving VVN {Id} by officer {Officer}", id, officerId);
                var result = await _service.Approve(id, officerId, new ApproveNotificationDTO { AssignedDockCode = dto.AssignedDockCode! }, _errors);
                if (result == null) return BadRequest(_errors);
                return Ok(result);
            }

            if (string.Equals(status, VvnStatus.Rejected.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(dto.Reason))
                    return BadRequest("Reason is required when rejecting");

                _logger.LogInformation("Rejecting VVN {Id} by officer {Officer}", id, officerId);
                var result = await _service.Reject(id, officerId, new RejectNotificationDTO { Reason = dto.Reason! }, _errors);
                if (result == null) return BadRequest(_errors);
                return Ok(result);
            }

            return BadRequest("Invalid status. Allowed values: Approved | Rejected");
        }

        // PATCH: api/vessel-visits/notifications/{id}/status
        // Updates status (used by Shipping Agent to submit: InProgress -> Submitted)
        public class UpdateVvnStatusDTO
        {
            public string Status { get; set; } = string.Empty; // e.g. "Submitted"
        }

        [HttpPatch("{id:long}/status")]
        public async Task<ActionResult<VesselVisitNotificationDTO>> UpdateStatus(long id, [FromBody] UpdateVvnStatusDTO dto)
        {
            if (dto == null) return BadRequest("Body is required");

            var status = dto.Status?.Trim();
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest("Status is required");

            _logger.LogInformation("Updating VVN {Id} status to {Status}", id, status);
            
            // For submit action (InProgress -> Submitted)
            if (string.Equals(status, VvnStatus.Submitted.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var result = await _service.Submit(id, _errors);
                if (result == null) return BadRequest(_errors);
                return Ok(result);
            }

            return BadRequest("Invalid status transition. Use PATCH /decision for approval/rejection.");
        }

        // PUT: api/vessel-visits/notifications/{id}
        [HttpPut("{id:long}")]
        public async Task<ActionResult<VesselVisitNotificationDTO>> Update(long id, VesselVisitNotificationDTO dto)
        {
            _logger.LogInformation("Updating Vessel Visit Notification {Id}", id);

            var vvn = await _service.GetById(id);
            if (vvn == null)
                return NotFound("Vessel Visit Notification not found.");

            // Only allow updates when status is InProgress
            if (!string.Equals(vvn.Status, VvnStatus.InProgress.ToString(), StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only notifications in 'InProgress' status can be updated.");

            var updated = await _service.Update(id, dto, _errors);
            if (updated == null)
                return BadRequest(_errors);

            return Ok(updated);
        }

        // GET with filters
        // GET: api/vessel-visits/notifications?vesselImoNumber=...&status=...&etaFrom=...&etaTo=...
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VesselVisitNotificationDTO>>> GetAll(
            [FromQuery] string? vesselImoNumber = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? etaFrom = null,
            [FromQuery] DateTime? etaTo = null)
        {
            _logger.LogInformation("Getting vessel visit notifications with filters");
            
            var filter = new VesselVisitNotificationFilterDTO
            {
                VesselImoNumber = vesselImoNumber,
                Status = status,
                DateFrom = etaFrom,
                DateTo = etaTo
            };

            var list = await _service.GetFiltered(filter);
            return Ok(list);
        }

        // GET: api/vessel-visits/notifications/my-notifications
        // US 2.2.10: Representative views their own submitted notifications
        [HttpGet("my-notifications")]
        public async Task<ActionResult<IEnumerable<VesselVisitNotificationDTO>>> GetMyNotifications()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("GetMyNotifications: No email claim found");
                return Unauthorized("User email not found in authentication claims");
            }

            var representative = await _representativeRepo.GetByEmailAsync(email);
            if (representative == null)
            {
                _logger.LogWarning("GetMyNotifications: No representative found for email {Email}", email);
                return StatusCode(403, $"No representative found for email {email}");
            }

            _logger.LogInformation("Getting notifications for representative {RepId}", representative.Id);
            var notifications = await _service.GetByRepresentativeIdAsync(representative.Id);
            return Ok(notifications);
        }

        // GET: api/vessel-visits/notifications/organization-notifications
        // US 2.2.10: Representative views notifications from same organization
        [HttpGet("organization-notifications")]
        public async Task<ActionResult<IEnumerable<VesselVisitNotificationDTO>>> GetOrganizationNotifications()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("GetOrganizationNotifications: No email claim found");
                return Unauthorized("User email not found in authentication claims");
            }

            var representative = await _representativeRepo.GetByEmailAsync(email);
            if (representative == null)
            {
                _logger.LogWarning("GetOrganizationNotifications: No representative found for email {Email}", email);
                return StatusCode(403, $"No representative found for email {email}");
            }

            _logger.LogInformation("Getting notifications for organization {OrgId}", representative.OrganizationId);
            var notifications = await _service.GetByOrganizationIdAsync(representative.OrganizationId);
            return Ok(notifications);
        }

        // GET: api/vessel-visits/notifications/{id}/audit
        [HttpGet("{id:long}/audit")]
        public async Task<ActionResult<List<DecisionLogDTO>>> GetAudit(long id)
        {
            _logger.LogInformation("Getting audit trail for notification {Id}", id);
            var audit = await _service.GetAudit(id);
            return Ok(audit);
        }
    }
}
