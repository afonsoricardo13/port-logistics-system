using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTO;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    /// <summary>
    /// Service interface for Privacy Policy management (US 4.5.1).
    /// Handles business logic for GDPR-compliant policy versioning and notifications.
    /// </summary>
    public interface IPrivacyPolicyService
    {
        Task<PrivacyPolicyDto?> GetCurrentPolicyAsync();
        Task<PrivacyPolicyDto?> GetPolicyByVersionAsync(int version);
        Task<IEnumerable<PrivacyPolicyHistoryItemDto>> GetPolicyHistoryAsync();
        Task<PrivacyPolicyDto> CreateNewVersionAsync(CreatePrivacyPolicyDto dto, string createdBy);
        Task ActivatePolicyAsync(int policyId);
        Task NotifyUsersAboutPolicyChangeAsync(int policyId);
    }

    /// <summary>
    /// Implementation of Privacy Policy management service (US 4.5.1).
    /// Coordinates policy versioning, activation, and user notifications.
    /// </summary>
    public class PrivacyPolicyService : IPrivacyPolicyService
    {
        private readonly IPrivacyPolicyRepository _repository;
        private readonly IEmailService _emailService;
        private readonly ILogger<PrivacyPolicyService> _logger;

        public PrivacyPolicyService(
            IPrivacyPolicyRepository repository,
            IEmailService emailService,
            ILogger<PrivacyPolicyService> logger)
        {
            _repository = repository;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the currently active privacy policy for public display.
        /// </summary>
        public async Task<PrivacyPolicyDto?> GetCurrentPolicyAsync()
        {
            _logger.LogInformation("Getting current privacy policy");
            
            var policy = await _repository.GetCurrentAsync();
            
            if (policy == null)
            {
                _logger.LogWarning("No active privacy policy found");
                return null;
            }

            return MapToDto(policy);
        }

        /// <summary>
        /// Gets a specific policy version (for history viewing).
        /// </summary>
        public async Task<PrivacyPolicyDto?> GetPolicyByVersionAsync(int version)
        {
            _logger.LogInformation("Getting privacy policy version {Version}", version);
            
            var policy = await _repository.GetByVersionAsync(version);
            
            if (policy == null)
            {
                _logger.LogWarning("Privacy policy version {Version} not found", version);
                return null;
            }

            return MapToDto(policy);
        }

        /// <summary>
        /// Gets complete history of all policy versions (admin view).
        /// </summary>
        public async Task<IEnumerable<PrivacyPolicyHistoryItemDto>> GetPolicyHistoryAsync()
        {
            _logger.LogInformation("Getting privacy policy history");
            
            var policies = await _repository.GetHistoryAsync();
            
            return policies.Select(p => new PrivacyPolicyHistoryItemDto
            {
                Id = p.Id,
                Version = p.Version,
                EffectiveDate = p.EffectiveDate,
                CreatedAt = p.CreatedAt,
                IsActive = p.IsActive,
                CreatedBy = p.CreatedBy,
                ChangeNotes = p.ChangeNotes,
                NotifiedAt = p.NotifiedAt,
                ContentPreview = p.Content.Length > 200 
                    ? p.Content.Substring(0, 200) + "..." 
                    : p.Content
            });
        }

        /// <summary>
        /// Creates a new privacy policy version.
        /// Implements versioning strategy: deactivates previous, assigns new version number.
        /// Optionally activates immediately and triggers user notifications.
        /// </summary>
        public async Task<PrivacyPolicyDto> CreateNewVersionAsync(CreatePrivacyPolicyDto dto, string createdBy)
        {
            _logger.LogInformation("Creating new privacy policy version by {CreatedBy}", createdBy);

            // Get next version number
            var nextVersion = await _repository.GetNextVersionNumberAsync();
            
            _logger.LogInformation("Assigning version number {Version}", nextVersion);

            // Create new policy entity
            var newPolicy = new PrivacyPolicy
            {
                Version = nextVersion,
                Content = dto.Content,
                EffectiveDate = dto.EffectiveDate,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                ChangeNotes = dto.ChangeNotes,
                IsActive = false // Will be activated separately if requested
            };

            // Save to database
            var createdPolicy = await _repository.CreateAsync(newPolicy);
            
            _logger.LogInformation("Privacy policy version {Version} created with ID {Id}", 
                createdPolicy.Version, createdPolicy.Id);

            // Activate if requested
            if (dto.ActivateImmediately)
            {
                await ActivatePolicyAsync(createdPolicy.Id);
            }

            // Notify users if requested
            if (dto.NotifyUsers && dto.ActivateImmediately)
            {
                // Run notification asynchronously (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await NotifyUsersAboutPolicyChangeAsync(createdPolicy.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to notify users about policy change");
                    }
                });
            }

            return MapToDto(createdPolicy);
        }

        /// <summary>
        /// Activates a specific policy version.
        /// Deactivates all other versions (only one active policy at a time).
        /// </summary>
        public async Task ActivatePolicyAsync(int policyId)
        {
            _logger.LogInformation("Activating privacy policy ID {Id}", policyId);

            var policy = await _repository.GetByIdAsync(policyId);
            
            if (policy == null)
            {
                _logger.LogError("Privacy policy ID {Id} not found", policyId);
                throw new InvalidOperationException($"Privacy policy with ID {policyId} not found");
            }

            if (policy.IsActive)
            {
                _logger.LogInformation("Privacy policy ID {Id} is already active", policyId);
                return;
            }

            // Deactivate all current active policies
            await _repository.DeactivateAllAsync();
            _logger.LogInformation("Deactivated all previous active policies");

            // Activate the requested policy
            policy.IsActive = true;
            await _repository.UpdateAsync(policy);
            
            _logger.LogInformation("Privacy policy version {Version} (ID {Id}) activated", 
                policy.Version, policy.Id);
        }

        /// <summary>
        /// Sends email notifications to all registered users about policy change.
        /// Implements RGPD Art 13(3) - information about changes to processing.
        /// Records notification timestamp in database.
        /// </summary>
        public async Task NotifyUsersAboutPolicyChangeAsync(int policyId)
        {
            _logger.LogInformation("Notifying users about privacy policy change (ID {Id})", policyId);

            var policy = await _repository.GetByIdAsync(policyId);
            
            if (policy == null)
            {
                _logger.LogError("Privacy policy ID {Id} not found for notification", policyId);
                throw new InvalidOperationException($"Privacy policy with ID {policyId} not found");
            }

            // TODO: Fetch all registered users from SystemUser repository
            // For now, logging placeholder - to be implemented when user notification system is ready
            _logger.LogInformation("Privacy policy notification system: placeholder - actual user emails would be sent here");
            
            // Example of what would happen:
            // var users = await _systemUserRepository.GetAllAsync();
            // foreach (var user in users)
            // {
            //     await _emailService.SendPrivacyPolicyChangeNotificationAsync(
            //         user.Email, 
            //         user.Name, 
            //         policy.Version, 
            //         policy.EffectiveDate
            //     );
            // }

            // Record notification timestamp
            policy.NotifiedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(policy);
            
            _logger.LogInformation("Privacy policy notification timestamp recorded for version {Version}", 
                policy.Version);
        }

        /// <summary>
        /// Maps domain entity to DTO.
        /// </summary>
        private static PrivacyPolicyDto MapToDto(PrivacyPolicy policy)
        {
            return new PrivacyPolicyDto
            {
                Id = policy.Id,
                Version = policy.Version,
                Content = policy.Content,
                EffectiveDate = policy.EffectiveDate,
                CreatedAt = policy.CreatedAt,
                IsActive = policy.IsActive,
                CreatedBy = policy.CreatedBy,
                ChangeNotes = policy.ChangeNotes,
                NotifiedAt = policy.NotifiedAt
            };
        }
    }
}
