using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using WebApi.Authorization;
using System.Security.Claims;

namespace WebApi.Controllers;

/// <summary>
/// Controller for user management
/// US 3.2.5: Administrator can create/update users and assign roles
/// US 3.2.6: User activation via email link
/// US 3.2.4: Restricted to Administrator role
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        IUserManagementService userManagementService,
        ILogger<UserManagementController> _logger)
    {
        _userManagementService = userManagementService;
        this._logger = _logger;
    }

    /// <summary>
    /// GET: api/usermanagement
    /// Get all system users (Administrator only)
    /// </summary>
    [HttpGet]
    [RequireRole("Administrator")]
    public async Task<ActionResult<List<SystemUserDTO>>> GetAllUsers()
    {
        try
        {
            var users = await _userManagementService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new { message = "Error retrieving users" });
        }
    }

    /// <summary>
    /// GET: api/usermanagement/{email}
    /// Get user by email
    /// </summary>
    [HttpGet("{email}")]
    [RequireRole("Administrator")]
    public async Task<ActionResult<SystemUserDTO>> GetUserByEmail(string email)
    {
        try
        {
            var user = await _userManagementService.GetUserByEmailAsync(email);
            
            if (user == null)
            {
                return NotFound(new { message = $"User with email {email} not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {Email}", email);
            return StatusCode(500, new { message = "Error retrieving user" });
        }
    }

    /// <summary>
    /// POST: api/usermanagement
    /// Create a new system user (Administrator only)
    /// AC: Administrator can assign internal role(s) to a user
    /// AC: By default, users are set to "deactivated" status
    /// AC: When authorizing user for first time, unique activation link is sent
    /// </summary>
    [HttpPost]
    [RequireRole("Administrator")]
    public async Task<ActionResult<SystemUserDTO>> CreateUser([FromBody] CreateSystemUserDTO dto, [FromQuery] string? createdBy = null)
    {
        // TODO US 3.2.4: Add [Authorize(Roles = "Administrator")] and get createdBy from authenticated user claims
        try
        {
            // If createdBy not provided via query, take from authenticated user claims
            if (string.IsNullOrWhiteSpace(createdBy))
            {
                createdBy = User.FindFirst(ClaimTypes.Email)?.Value;
            }
            if (string.IsNullOrWhiteSpace(createdBy))
            {
                return Unauthorized(new { message = "Not authenticated" });
            }

            var user = await _userManagementService.CreateUserAsync(dto, createdBy);
            return CreatedAtAction(nameof(GetUserByEmail), new { email = user.Email }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating user");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Error creating user" });
        }
    }

    /// <summary>
    /// PUT: api/usermanagement
    /// Update an existing system user (Administrator only)
    /// AC: Administrator can update internal role(s) of a given user
    /// </summary>
    [HttpPut]
    [RequireRole("Administrator")]
    public async Task<ActionResult<SystemUserDTO>> UpdateUser([FromBody] UpdateSystemUserDTO dto)
    {
        // TODO US 3.2.4: Add [Authorize(Roles = "Administrator")] attribute for backend authorization
        try
        {
            var user = await _userManagementService.UpdateUserAsync(dto);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation updating user");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user");
            return StatusCode(500, new { message = "Error updating user" });
        }
    }

    /// <summary>
    /// POST: api/usermanagement/{email}/resend-activation
    /// Resend activation email to user
    /// </summary>
    [HttpPost("{email}/resend-activation")]
    [RequireRole("Administrator")]
    public async Task<ActionResult> ResendActivationEmail(string email)
    {
        // TODO US 3.2.4: Add [Authorize(Roles = "Administrator")] attribute for backend authorization
        try
        {
            var success = await _userManagementService.ResendActivationEmailAsync(email);
            
            if (!success)
            {
                return NotFound(new { message = $"User with email {email} not found" });
            }

            return Ok(new { message = "Activation email sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending activation email");
            return StatusCode(500, new { message = "Error sending activation email" });
        }
    }

    /// <summary>
    /// GET: api/usermanagement/validate-token/{token}
    /// Validate activation token status WITHOUT authentication
    /// This allows checking if token is already used/expired before requiring login
    /// </summary>
    [HttpGet("validate-token/{token}")]
    public async Task<ActionResult> ValidateActivationToken(string token)
    {
        try
        {
            var status = await _userManagementService.ValidateActivationTokenAsync(token);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating activation token");
            return StatusCode(500, new { message = "Error validating token" });
        }
    }

    /// <summary>
    /// POST: api/usermanagement/activate
    /// Activate user account with token
    /// US 3.2.6: User clicks activation link from email
    /// AC: System confirms authenticated user matches link identity
    /// AC: Token must be valid and not expired
    /// </summary>
    [HttpPost("activate")]
    public async Task<ActionResult> ActivateUser([FromBody] ActivateUserDTO dto)
    {
        try
        {
            // Must be authenticated; enforce that the authenticated user matches link identity
            var authEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(authEmail))
            {
                return Unauthorized(new { message = "Not authenticated" });
            }

            // Force the DTO email to the authenticated user's email to prevent impersonation
            dto.Email = authEmail.Trim().ToLowerInvariant();

            var success = await _userManagementService.ActivateUserAsync(dto);
            
            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired activation token" });
            }

            return Ok(new { message = "Account activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user");
            return StatusCode(500, new { message = "Error activating user account" });
        }
    }
}
