using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Model
{
    /// <summary>
    /// Represents a version of the Privacy Policy document.
    /// Implements versioning and historical tracking for GDPR compliance (US 4.5.1).
    /// Based on RGPD Articles 13-14 (information obligations).
    /// </summary>
    public class PrivacyPolicy
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Sequential version number (1, 2, 3, ...)
        /// </summary>
        [Required]
        public int Version { get; set; }

        /// <summary>
        /// Complete HTML or Markdown content of the privacy policy.
        /// Must include: controller identity, purposes, legal basis, data categories,
        /// recipients, retention periods, data subject rights (RGPD Art 13-14).
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Date when this version becomes effective (can be future date).
        /// </summary>
        [Required]
        public DateTime EffectiveDate { get; set; }

        /// <summary>
        /// Timestamp when this version was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Indicates if this is the currently active policy version.
        /// Only one version should be active at a time.
        /// </summary>
        [Required]
        public bool IsActive { get; set; }

        /// <summary>
        /// Username or identifier of the admin who created this version.
        /// For accountability (RGPD Art 5.2 - responsibility principle).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Optional notes or change summary for internal tracking.
        /// </summary>
        [MaxLength(1000)]
        public string? ChangeNotes { get; set; }

        /// <summary>
        /// Date when users were notified about this version (null if not yet notified).
        /// </summary>
        public DateTime? NotifiedAt { get; set; }

        public PrivacyPolicy()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = false;
        }
    }
}
