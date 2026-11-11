using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Application.DTO;
using System.Security.Claims;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RoleAssignmentService _roleService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(RoleAssignmentService roleService, ILogger<AuthController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user profile with role and permissions
        /// US 3.2.2: Return internal role after authentication
        /// US 3.2.4: Provide permissions for frontend authorization
        /// </summary>
        [HttpGet("me")]
        public ActionResult<UserProfileDTO> GetCurrentUser()
        {
            // Try to get email from claims first (dev auth middleware or JWT)
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            // If not in claims, try to get from header (Azure/production with OAuth frontend)
            if (string.IsNullOrWhiteSpace(email))
            {
                email = Request.Headers["X-User-Email"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("GetCurrentUser: No email in claims or header");
                return Unauthorized(new { error = "Not authenticated" });
            }

            var profile = _roleService.GetUserProfile(email, name);

            if (profile == null)
            {
                _logger.LogWarning("GetCurrentUser: No role assigned for {Email}", email);
                return Forbid();
            }

            _logger.LogInformation("GetCurrentUser: Returned profile for {Email} as {Role}", 
                profile.Email, profile.Role);

            return Ok(profile);
        }

        /// <summary>
        /// GET: api/auth/oem
        /// Gets user profile from X-User-Email header (for OEM backend integration)
        /// This endpoint is called by the OEM backend to validate users
        /// </summary>
        [HttpGet("oem")]
        public ActionResult GetUserForOEM()
        {
            // Get email from header (set by OEM backend)
            var email = Request.Headers["X-User-Email"].FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("GET /api/auth/oem called without X-User-Email header");
                return Unauthorized(new { message = "X-User-Email header is required" });
            }

            _logger.LogInformation("GET /api/auth/oem called for user: {Email}", email);

            var profile = _roleService.GetUserProfile(email, null);

            if (profile == null || !profile.Active)
            {
                _logger.LogWarning("User not found or inactive: {Email}", email);
                return Unauthorized(new { message = "User not found or inactive" });
            }

            _logger.LogInformation("✅ Returning user profile for {Email} with role {Role}", email, profile.Role);

            // Return in format expected by OEM backend
            return Ok(new
            {
                email = profile.Email,
                name = profile.Name,
                role = profile.Role,
                active = profile.Active,
                permissions = profile.Permissions
            });
        }

        /// <summary>
        /// Test endpoint to simulate login with specific email (dev only)
        /// Sets X-User-Email header for DevAuthenticationMiddleware
        /// </summary>
        [HttpPost("test-login")]
        public ActionResult TestLogin([FromBody] TestLoginRequest request)
        {
            _logger.LogInformation("TestLogin: Simulating login for {Email}", request.Email);
            return Ok(new { message = "Use this email in X-User-Email header", email = request.Email });
        }
    }

    public class TestLoginRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
