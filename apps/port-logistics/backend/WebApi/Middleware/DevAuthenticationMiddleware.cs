using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApi.Middleware
{
    /// <summary>
    /// Middleware for local development authentication
    /// Populates user claims from email header for testing without external IAM
    /// US 3.2.2: Simulate external authentication and load internal role
    /// US 3.2.4: Populate claims for RBAC/ABAC enforcement
    /// </summary>
    public class DevAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DevAuthenticationMiddleware> _logger;

        public DevAuthenticationMiddleware(RequestDelegate next, ILogger<DevAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RoleAssignmentService roleService)
        {
            // Skip authentication for /api/auth/oem endpoint (used by OEM backend)
            if (context.Request.Path.StartsWithSegments("/api/auth/oem") || 
                context.Request.Path.StartsWithSegments("/api/auth/me"))
            {
                await _next(context);
                return;
            }

            // Check if user already authenticated
            if (context.User?.Identity?.IsAuthenticated ?? false)
            {
                await _next(context);
                return;
            }

            // Get email from header (simulating external IAM)
            var email = context.Request.Headers["X-User-Email"].FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogDebug("DevAuth: No X-User-Email header found");
                await _next(context);
                return;
            }

            // US 3.2.6: For activation endpoint, authenticate even without active profile
            var isActivationEndpoint = context.Request.Path.StartsWithSegments("/api/usermanagement/activate");
            
            // Get user profile with role and permissions
            var profile = roleService.GetUserProfile(email);
            
            if (profile == null && !isActivationEndpoint)
            {
                _logger.LogWarning("DevAuth: No role assigned for email {Email}", email);
                await _next(context);
                return;
            }

            // Create claims
            // US 3.2.6: For activation endpoint without profile, create minimal claims with just email
            Claim[] claims;
            if (profile == null && isActivationEndpoint)
            {
                claims = new[]
                {
                    new Claim(ClaimTypes.Email, email)
                };
                _logger.LogInformation("DevAuth: Authenticated {Email} for activation (no profile yet)", email);
            }
            else
            {
                claims = new[]
                {
                    new Claim(ClaimTypes.Name, profile!.Email),
                    new Claim(ClaimTypes.Email, profile.Email),
                    new Claim(ClaimTypes.Role, profile.Role),
                    new Claim("name", profile.Name)
                }.Concat(profile.Permissions.Select(p => new Claim("permission", p))).ToArray();
                
                _logger.LogInformation("DevAuth: Authenticated {Email} as {Role} with {Count} permissions",
                    profile.Email, profile.Role, profile.Permissions.Length);
            }

            var identity = new ClaimsIdentity(claims, "DevAuth");
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }
    }
}
