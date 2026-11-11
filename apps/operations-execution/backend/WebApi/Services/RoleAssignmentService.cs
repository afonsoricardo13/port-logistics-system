using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApi.Services
{
    /// <summary>
    /// Service for managing user role assignments and permissions
    /// US 4.1.1: Map users to roles and permissions for RBAC/ABAC
    /// Connects to Port Logistics database to load user roles dynamically
    /// </summary>
    public class RoleAssignmentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RoleAssignmentService> _logger;
        private readonly string _portLogisticsConnectionString;

        public RoleAssignmentService(
            IConfiguration configuration,
            ILogger<RoleAssignmentService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Get Port Logistics database connection string
            _portLogisticsConnectionString = configuration.GetConnectionString("PortLogisticsDatabase") 
                ?? throw new InvalidOperationException("PortLogisticsDatabase connection string not found in configuration");
            
            _logger.LogInformation("RoleAssignmentService initialized with Port Logistics database");
        }

        /// <summary>
        /// Get user profile by querying Port Logistics database
        /// Loads role from SystemUsers table and maps to OEM permissions
        /// </summary>
        public async Task<UserProfile?> GetUserProfileAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            try
            {
                email = email.Trim().ToLowerInvariant();
                
                _logger.LogInformation("🔍 Attempting to load user from Port Logistics database: {Email}", email);
                _logger.LogDebug("Using connection string: {ConnectionString}", 
                    System.Text.RegularExpressions.Regex.Replace(_portLogisticsConnectionString, "Password=[^;]*", "Password=***"));

                // Create DbContext for Port Logistics database
                var optionsBuilder = new DbContextOptionsBuilder<PortLogisticsDbContext>();
                optionsBuilder.UseMySql(_portLogisticsConnectionString, ServerVersion.AutoDetect(_portLogisticsConnectionString));

                using var context = new PortLogisticsDbContext(optionsBuilder.Options);
                
                _logger.LogDebug("DbContext created, querying SystemUsers table...");

                // Query SystemUsers table
                var systemUser = await context.SystemUsers
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (systemUser == null)
                {
                    _logger.LogWarning("❌ User not found in Port Logistics database: {Email}", email);
                    return null;
                }

                _logger.LogInformation("📥 User found in database: {Email}, Role: {Role}, Active: {Active}", 
                    systemUser.Email, systemUser.Role, systemUser.Active);

                if (!systemUser.Active)
                {
                    _logger.LogWarning("❌ User inactive in Port Logistics database: {Email}", email);
                    return null;
                }

                // Map role to OEM permissions
                var permissions = GetPermissionsForRole(systemUser.Role);

                _logger.LogInformation("✅ User authenticated successfully: {Email} with role {Role} and {Count} permissions",
                    email, systemUser.Role, permissions.Length);

                return new UserProfile
                {
                    Email = systemUser.Email,
                    Name = systemUser.Name ?? systemUser.Email,
                    Role = systemUser.Role,
                    Permissions = permissions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting user profile from database for {Email}. Exception: {Message}", 
                    email, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Synchronous wrapper for backward compatibility
        /// </summary>
        public UserProfile? GetUserProfile(string email)
        {
            return GetUserProfileAsync(email).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Map Port Logistics roles to OEM permissions
        /// </summary>
        private string[] GetPermissionsForRole(string role)
        {
            return role switch
            {
                "Administrator" => new[]
                {
                    "operation-plans", "vessel-visit-executions", "executed-operations", 
                    "incidents", "incident-types", "complementary-tasks", 
                    "complementary-task-categories", "analytics"
                },
                "LogisticsOperator" => new[]
                {
                    "operation-plans", "vessel-visit-executions", "executed-operations", 
                    "incidents", "complementary-tasks", "analytics"
                },
                "PortAuthorityOfficer" => new[]
                {
                    "operation-plans:read", "vessel-visit-executions:read", 
                    "incidents:read", "incident-types"
                },
                "PortOperationsSupervisor" => new[]
                {
                    "complementary-task-categories"
                },
                "ShippingAgentRepresentative" => new[]
                {
                    "operation-plans:read", "vessel-visit-executions:read"
                },
                _ => Array.Empty<string>()
            };
        }

        public IEnumerable<UserProfile> GetAllUsers()
        {
            return Array.Empty<UserProfile>();
        }
    }

    /// <summary>
    /// DbContext for accessing Port Logistics database (SystemUsers table only)
    /// </summary>
    public class PortLogisticsDbContext : DbContext
    {
        public PortLogisticsDbContext(DbContextOptions<PortLogisticsDbContext> options) : base(options)
        {
        }

        public DbSet<SystemUser> SystemUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemUser>(entity =>
            {
                entity.ToTable("SystemUsers");
                entity.HasKey(e => e.Email); // Email is the primary key, not Id
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Role).IsRequired();
                entity.Property(e => e.Active).IsRequired();
            });
        }
    }

    /// <summary>
    /// Entity for SystemUsers table in Port Logistics database
    /// </summary>
    public class SystemUser
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    public class UserProfile
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string[] Permissions { get; set; } = Array.Empty<string>();
    }
}
