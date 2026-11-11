using Application.DTO;
using DataModel.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Service for managing user authorization
/// US 3.2.2: Load internal authorization role after IAM authentication
/// US 3.2.5: Roles are managed by Administrator via database (SystemUsers table)
/// US 3.2.6: Respects user activation status
/// </summary>
public class AuthorizationService
{
    private readonly AbsanteeContext _context;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        AbsanteeContext context,
        ILogger<AuthorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get user authorization by email
    /// US 3.2.2: After IAM login, the SPA must call backend to retrieve the user's assigned role
    /// US 3.2.5: Roles are managed by Administrator via database
    /// US 3.2.6: If the user has no assigned role or is inactive, access must be denied
    /// </summary>
    public async Task<UserAuthorizationDTO?> GetUserAuthorizationAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("GetUserAuthorization called with empty email");
            return null;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        
        // Get user from database (SystemUsers table)
        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
        {
            _logger.LogInformation("User not found in database: {Email}", email);
            return new UserAuthorizationDTO
            {
                Email = email,
                Role = string.Empty,
                Active = false,
                Message = "Access denied: No role assigned to this user. Please contact an Administrator."
            };
        }

        if (!user.Active)
        {
            _logger.LogInformation("User inactive in database: {Email}", email);
            return new UserAuthorizationDTO
            {
                Email = email,
                Role = user.Role,
                Active = false,
                Message = "Access denied: User account is inactive. Please check your activation email."
            };
        }

        _logger.LogInformation("User authorized: {Email} with role {Role}", email, user.Role);
        
        return new UserAuthorizationDTO
        {
            Email = email,
            Role = user.Role,
            Active = true,
            Message = null
        };
    }
}
