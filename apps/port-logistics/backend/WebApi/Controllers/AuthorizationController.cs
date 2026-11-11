using Application.DTO;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Controller for user authorization
/// US 3.2.2: Automatically load internal authorization role after authentication
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly AuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        AuthorizationService authorizationService,
        ILogger<AuthorizationController> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// GET: api/authorization/role?email=user@example.com
    /// Retrieves the user's assigned role after IAM authentication
    /// 
    /// AC: After IAM login, the SPA must call a backend endpoint to retrieve the user's assigned role
    /// AC: If the user has no assigned role or it is inactive, access must be denied with an appropriate message
    /// </summary>
    /// <param name="email">User email from IAM provider (Google)</param>
    /// <returns>User authorization information including role and active status</returns>
    [HttpGet("role")]
    public async Task<ActionResult<UserAuthorizationDTO>> GetUserRole([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email parameter is required" });
        }

        var authorization = await _authorizationService.GetUserAuthorizationAsync(email);

        if (authorization == null)
        {
            _logger.LogWarning("Authorization not found for email: {Email}", email);
            return NotFound(new UserAuthorizationDTO
            {
                Email = email,
                Role = string.Empty,
                Active = false,
                Message = "Access denied: User not found in system"
            });
        }

        // If user has no role or is inactive, return 403 Forbidden
        if (string.IsNullOrEmpty(authorization.Role) || !authorization.Active)
        {
            _logger.LogInformation("Access denied for user: {Email}, Active: {Active}, Role: {Role}",
                email, authorization.Active, authorization.Role);
            return StatusCode(403, authorization);
        }

        return Ok(authorization);
    }
}
