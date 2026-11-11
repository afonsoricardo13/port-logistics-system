using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace WebApi.Authorization
{
    /// <summary>
    /// Authorization attribute to enforce role-based access control
    /// US 3.2.4: Restrict actions based on user roles
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public RequireRoleAttribute(params string[] roles)
        {
            _allowedRoles = roles ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var logger = context.HttpContext.RequestServices
                .GetService(typeof(ILogger<RequireRoleAttribute>)) as ILogger<RequireRoleAttribute>;
            
            var user = context.HttpContext.User;
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                logger?.LogWarning("RequireRole: Unauthenticated access attempt to {Path}", 
                    context.HttpContext.Request.Path);
                context.Result = new UnauthorizedResult();
                return;
            }
            
            var role = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
            
            if (string.IsNullOrEmpty(role) || !_allowedRoles.Contains(role))
            {
                logger?.LogWarning(
                    "RequireRole: Unauthorized access by {User} (role: {Role}) to {Path} (required: {Required})",
                    user.Identity?.Name, role, context.HttpContext.Request.Path, string.Join(",", _allowedRoles));
                context.Result = new ForbidResult();
            }
        }
    }
}
