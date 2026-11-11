using Domain.Model;

namespace Domain.IRepository
{
    /// <summary>
    /// Repository interface for Privacy Policy management (US 4.5.1).
    /// Provides CRUD operations, versioning, and historical tracking.
    /// </summary>
    public interface IPrivacyPolicyRepository
    {
        /// <summary>
        /// Gets the currently active privacy policy version.
        /// </summary>
        /// <returns>Active PrivacyPolicy or null if none exists</returns>
        Task<PrivacyPolicy?> GetCurrentAsync();

        /// <summary>
        /// Gets a specific privacy policy version by its ID.
        /// </summary>
        Task<PrivacyPolicy?> GetByIdAsync(int id);

        /// <summary>
        /// Gets a specific privacy policy by version number.
        /// </summary>
        Task<PrivacyPolicy?> GetByVersionAsync(int version);

        /// <summary>
        /// Gets all privacy policy versions ordered by version descending (newest first).
        /// </summary>
        Task<IEnumerable<PrivacyPolicy>> GetHistoryAsync();

        /// <summary>
        /// Creates a new privacy policy version.
        /// </summary>
        Task<PrivacyPolicy> CreateAsync(PrivacyPolicy policy);

        /// <summary>
        /// Updates an existing privacy policy (e.g., to mark as active or set notification date).
        /// </summary>
        Task<PrivacyPolicy> UpdateAsync(PrivacyPolicy policy);

        /// <summary>
        /// Deactivates all currently active policies.
        /// Used before activating a new version.
        /// </summary>
        Task DeactivateAllAsync();

        /// <summary>
        /// Gets the next available version number.
        /// </summary>
        Task<int> GetNextVersionNumberAsync();

        /// <summary>
        /// Checks if a specific version number already exists.
        /// </summary>
        Task<bool> VersionExistsAsync(int version);
    }
}
