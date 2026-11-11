using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace WebApi.Middleware
{
        public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtValidationMiddleware> _logger;
        private readonly string _googleClientId;
        private readonly string _googleIssuer1 = "https://accounts.google.com";
        private readonly string _googleIssuer2 = "accounts.google.com";
        private static TokenValidationParameters? _cachedValidationParameters;
        private static DateTime _cacheExpiry = DateTime.MinValue;

        public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            
            // Get Google OAuth2 client ID from configuration
            _googleClientId = configuration["Authentication:Google:ClientId"] 
                ?? throw new InvalidOperationException("Google Client ID not configured");
        }

        public async Task InvokeAsync(HttpContext context, RoleAssignmentService roleService)
        {
            // Skip validation if already authenticated (for chained middleware scenarios)
            if (context.User?.Identity?.IsAuthenticated ?? false)
            {
                await _next(context);
                return;
            }

            // Extract Bearer token from Authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("JWT: No Bearer token in Authorization header");
                await _next(context);
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug("JWT: Empty token after Bearer prefix");
                await _next(context);
                return;
            }

            try
            {
                // Validate token and extract claims
                var principal = await ValidateGoogleTokenAsync(token);
                if (principal == null)
                {
                    _logger.LogWarning("JWT: Token validation failed");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" });
                    return;
                }

                // Extract email from token claims
                var email = principal.FindFirst(ClaimTypes.Email)?.Value 
                    ?? principal.FindFirst("email")?.Value;
                
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("JWT: Token does not contain email claim");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { error = "Token missing email claim" });
                    return;
                }

                // US 3.2.6: For activation endpoint, authenticate even without active profile
                var isActivationEndpoint = context.Request.Path.StartsWithSegments("/api/usermanagement/activate");
                
                // Load user profile with internal role and permissions
                var profile = roleService.GetUserProfile(email);
                
                if (profile == null && !isActivationEndpoint)
                {
                    _logger.LogWarning("JWT: User {Email} has valid token but no internal role assigned", email);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "User not activated or role not assigned" });
                    return;
                }

                // Create claims with internal role and permissions
                Claim[] claims;
                if (profile == null && isActivationEndpoint)
                {
                    // Minimal claims for activation endpoint
                    claims = new[]
                    {
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Name, principal.FindFirst("name")?.Value ?? email)
                    };
                    _logger.LogInformation("JWT: Authenticated {Email} for activation (no profile yet)", email);
                }
                else
                {
                    // Full claims with role and permissions
                    claims = new[]
                    {
                        new Claim(ClaimTypes.Name, profile!.Email),
                        new Claim(ClaimTypes.Email, profile.Email),
                        new Claim(ClaimTypes.Role, profile.Role),
                        new Claim("name", profile.Name)
                    }.Concat(profile.Permissions.Select(p => new Claim("permission", p))).ToArray();
                    
                    _logger.LogInformation("JWT: Authenticated {Email} as {Role} with {Count} permissions",
                        profile.Email, profile.Role, profile.Permissions.Length);
                }

                var identity = new ClaimsIdentity(claims, "JwtBearer");
                context.User = new ClaimsPrincipal(identity);

                await _next(context);
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "JWT: Token expired");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Token expired" });
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "JWT: Token validation failed");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT: Unexpected error during token validation");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = "Token validation error" });
            }
        }

        /// <summary>
        /// Validates Google OAuth2 ID token using Google's public keys
        /// Checks signature, expiration, audience, and issuer
        /// </summary>
        private async Task<ClaimsPrincipal?> ValidateGoogleTokenAsync(string token)
        {
            try
            {
                // Get validation parameters (cached for performance)
                var validationParameters = await GetValidationParametersAsync();

                var tokenHandler = new JwtSecurityTokenHandler();
                
                // Validate token and return principal
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                // Additional validation: ensure it's a JWT token
                if (validatedToken is not JwtSecurityToken jwtToken)
                {
                    _logger.LogWarning("JWT: Token is not a valid JWT");
                    return null;
                }

                // Additional validation: check token type if present
                var tokenType = jwtToken.Header.Typ;
                if (!string.IsNullOrEmpty(tokenType) && !tokenType.Equals("JWT", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("JWT: Invalid token type: {Type}", tokenType);
                    return null;
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                throw; // Re-throw to be handled by caller
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "JWT: Token security validation failed");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT: Unexpected validation error");
                return null;
            }
        }

        /// <summary>
        /// Gets token validation parameters for Google OAuth2 tokens
        /// Uses Google's JWKS endpoint to fetch public keys (cached for 1 hour)
        /// </summary>
        private async Task<TokenValidationParameters> GetValidationParametersAsync()
        {
            // Return cached parameters if still valid (cache for 1 hour)
            if (_cachedValidationParameters != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedValidationParameters;
            }

            // Fetch Google's signing keys from JWKS endpoint
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://accounts.google.com/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());

            var discoveryDocument = await configurationManager.GetConfigurationAsync();

            // Create validation parameters
            _cachedValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[] { _googleIssuer1, _googleIssuer2 },
                
                ValidateAudience = true,
                ValidAudience = _googleClientId,
                
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = discoveryDocument.SigningKeys,
                
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew
                
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // Cache for 1 hour
            _cacheExpiry = DateTime.UtcNow.AddHours(1);
            
            _logger.LogInformation("JWT: Fetched and cached Google signing keys");

            return _cachedValidationParameters;
        }
    }
}
