using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace WebApi.Authorization
{
    /// <summary>
    /// Authorization attribute to enforce permission-based access control
    /// US 3.2.4: Restrict actions based on user permissions (ABAC)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _requiredPermissions;

        public RequirePermissionAttribute(params string[] permissions)
        {
            _requiredPermissions = permissions ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var logger = context.HttpContext.RequestServices
                .GetService(typeof(ILogger<RequirePermissionAttribute>)) as ILogger<RequirePermissionAttribute>;
            
            var user = context.HttpContext.User;
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                logger?.LogWarning("RequirePermission: Unauthenticated access attempt to {Path}", 
                    context.HttpContext.Request.Path);
                context.Result = new UnauthorizedResult();
                return;
            }
            
            var userPermissions = user.FindAll("permission")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            var missingPermissions = _requiredPermissions
                .Where(p => !userPermissions.Contains(p))
                .ToArray();
            
            if (missingPermissions.Any())
            {
                logger?.LogWarning(
                    "RequirePermission: Unauthorized access by {User} to {Path} (missing: {Missing})",
                    user.Identity?.Name, context.HttpContext.Request.Path, string.Join(",", missingPermissions));
                context.Result = new ForbidResult();
            }
        }
    }
}
