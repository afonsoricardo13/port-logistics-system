using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTO
{
    /// <summary>
    /// DTO for creating a new Privacy Policy version (US 4.5.1).
    /// Used by administrators to publish new policy versions.
    /// </summary>
    public class CreatePrivacyPolicyDto
    {
        /// <summary>
        /// Complete HTML or Markdown content of the privacy policy.
        /// Must comply with RGPD Articles 13-14 requirements.
        /// </summary>
        [Required(ErrorMessage = "Content is required")]
        [MinLength(100, ErrorMessage = "Content must be at least 100 characters")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Date when this version becomes effective (can be future date).
        /// </summary>
        [Required(ErrorMessage = "Effective date is required")]
        public DateTime EffectiveDate { get; set; }

        /// <summary>
        /// Optional notes explaining changes from previous version.
        /// </summary>
        [MaxLength(1000)]
        public string? ChangeNotes { get; set; }

        /// <summary>
        /// If true, immediately activates this policy and deactivates previous versions.
        /// If false, creates as draft (can be activated later).
        /// </summary>
        public bool ActivateImmediately { get; set; } = true;

        /// <summary>
        /// If true, triggers notification to all registered users about policy change.
        /// </summary>
        public bool NotifyUsers { get; set; } = true;
    }

    /// <summary>
    /// DTO for returning a Privacy Policy version.
    /// Used for both current policy (public) and history queries (admin).
    /// </summary>
    public class PrivacyPolicyDto
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ChangeNotes { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }

    /// <summary>
    /// DTO for Privacy Policy history list (admin view).
    /// Contains summary information without full content (for performance).
    /// </summary>
    public class PrivacyPolicyHistoryItemDto
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ChangeNotes { get; set; }
        public DateTime? NotifiedAt { get; set; }
        
        /// <summary>
        /// First 200 characters of content (preview).
        /// </summary>
        public string ContentPreview { get; set; } = string.Empty;
    }
}
