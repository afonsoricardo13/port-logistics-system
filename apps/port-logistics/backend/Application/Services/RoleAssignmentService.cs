using Microsoft.Extensions.Configuration;
using Application.DTO;
using System;
using System.Collections.Generic;
using DataModel.Repository;
using System.Linq;

namespace Application.Services
{
    /// <summary>
    /// Service to assign roles to authenticated users based on their email
    /// US 3.2.2: Load internal authorization role after external IAM authentication
    /// US 3.2.4: Provide role/permissions for RBAC enforcement
    /// US 3.2.5: Roles are managed by Administrator via database (SystemUsers table)
    /// </summary>
    public class RoleAssignmentService
    {
        private readonly AbsanteeContext _context;

        public RoleAssignmentService(AbsanteeContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get user profile with assigned role based on email
        /// US 3.2.2: Load internal authorization role after external IAM authentication
        /// US 3.2.5: Roles assigned via Administrator (stored in SystemUsers table)
        /// US 3.2.6: Respects user activation status
        /// </summary>
        public UserProfileDTO? GetUserProfile(string email, string? name = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            email = email.Trim().ToLowerInvariant();

            // Lookup from database (users created via UserManagement - US 3.2.5)
            var systemUser = _context.SystemUsers.FirstOrDefault(u => u.Email == email);
            if (systemUser == null)
            {
                return null; // User not found
            }

            // US 3.2.6: Check if user is active (activation process)
            if (!systemUser.Active)
            {
                return null; // Exists but inactive -> deny access
            }

            // Parse stored role string into enum (case-insensitive). If invalid -> deny.
            if (!Enum.TryParse<UserRole>(systemUser.Role, true, out var role))
            {
                return null; // Unknown role string
            }

            return new UserProfileDTO
            {
                Email = systemUser.Email,
                Name = name ?? systemUser.Name ?? systemUser.Email,
                Role = role.ToString(),
                Active = true,
                Permissions = GetPermissionsForRole(role)
            };
        }

        /// <summary>
        /// Get permissions/features available for each role
        /// US 3.2.4: Define granular permissions for ABAC
        /// </summary>
        private string[] GetPermissionsForRole(UserRole role)
        {
            return role switch
            {
                UserRole.Administrator => new[]
                {
                    "users", "vessel-types", "vessels", "docks", "storage-areas",
                    "organizations", "representatives", "vvn-review", "staff",
                    "resources", "qualifications", "vessel-visit-notifications", "scheduling",
                    "incident-types", "incidents", "task-categories", "complementary-tasks",
                    "operation-plans", "vessel-visit-executions"
                },
                UserRole.PortAuthorityOfficer => new[]
                {
                    "vessel-types", "vessels", "docks", "storage-areas",
                    "organizations", "representatives", "vvn-review",
                    "vessel-visit-notifications", "incident-types"
                },
                UserRole.ShippingAgentRepresentative => new[]
                {
                    "vessel-visit-notifications"
                },
                UserRole.LogisticsOperator => new[]
                {
                    "staff", "resources", "qualifications", "scheduling",
                    "operation-plans", "vessel-visit-executions", "incidents", "complementary-tasks"
                },
                UserRole.PortOperationsSupervisor => new[]
                {
                    "task-categories"
                },
                _ => Array.Empty<string>()
            };
        }
    }

    /// <summary>
    /// User roles matching Domain.Model.UserRole
    /// Duplicated here to avoid domain dependency in Application layer
    /// </summary>
    public enum UserRole
    {
        Administrator = 0,
        PortAuthorityOfficer = 1,
        ShippingAgentRepresentative = 2,
        LogisticsOperator = 3,
        PortOperationsSupervisor = 4
    }
}
