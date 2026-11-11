using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApi.Services;

namespace WebApi.Middleware
{
    /// <summary>
    /// Middleware for local development authentication
    /// Populates user claims from email header for testing without external IAM
    /// US 4.1.1: Simulate external authentication and load internal role
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
            _logger.LogInformation("🔍 DevAuth OEM: Processing request for {Path}", context.Request.Path);
            
            // Check if user already authenticated
            if (context.User?.Identity?.IsAuthenticated ?? false)
            {
                _logger.LogDebug("DevAuth: User already authenticated as {User}", context.User.Identity.Name);
                await _next(context);
                return;
            }

            // Get email from header (simulating external IAM)
            var email = context.Request.Headers["X-User-Email"].FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("❌ DevAuth OEM: No X-User-Email header found for {Path}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Missing X-User-Email header" });
                return;
            }

            _logger.LogInformation("🔐 DevAuth OEM: Attempting authentication for {Email}", email);

            // When running integration tests, avoid external calls to Port Logistics API
            if (Environment.GetEnvironmentVariable("ASPNETCORE_INTEGRATION_TEST") == "true")
            {
                _logger.LogInformation("DevAuth: Integration test mode - granting default test role for {Email}", email);

                var defaultPermissions = new[] { "operation-plans", "vessel-visit-executions" };
                var claimsTest = new[]
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, "LogisticsOperator"),
                    new Claim("name", "Integration Test User")
                }.Concat(defaultPermissions.Select(p => new Claim("permission", p))).ToArray();

                var identityTest = new ClaimsIdentity(claimsTest, "DevAuth");
                context.User = new ClaimsPrincipal(identityTest);

                await _next(context);
                return;
            }

            // Get user profile with role and permissions
            try
            {
                var profile = await roleService.GetUserProfileAsync(email);
                
                if (profile == null)
                {
                    _logger.LogWarning("❌ DevAuth OEM: No role assigned for email {Email}", email);
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = $"No role assigned for {email}" });
                    return;
                }

                // Create claims
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, profile.Email),
                    new Claim(ClaimTypes.Email, profile.Email),
                    new Claim(ClaimTypes.Role, profile.Role),
                    new Claim("name", profile.Name)
                }.Concat(profile.Permissions.Select(p => new Claim("permission", p))).ToArray();
                
                _logger.LogInformation("✅ DevAuth OEM: Authenticated {Email} as {Role} with {Count} permissions",
                    profile.Email, profile.Role, profile.Permissions.Length);

                var identity = new ClaimsIdentity(claims, "DevAuth");
                context.User = new ClaimsPrincipal(identity);

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ DevAuth OEM: Failed to authenticate {Email}", email);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "Authentication service failed", details = ex.Message });
                return;
            }

        }
    }
}
