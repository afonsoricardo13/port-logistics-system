using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Model
{
    /// <summary>
    /// Represents a user's request to exercise GDPR data rights (US 4.5.3).
    /// Implements tracking for RGPD Articles 15, 16, 17 requests.
    /// </summary>
    [Table("datarightsrequests")]
    public class DataRightsRequest
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// Type of request: 0=Access, 1=Rectification, 2=Deletion.
        /// </summary>
        [Required]
        [Column("RequestType")]
        public int RequestType { get; set; }

        /// <summary>
        /// Email of the user making the request.
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("UserEmail")]
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Status: 0=Pending, 1=InProgress, 2=Completed, 3=Rejected.
        /// </summary>
        [Required]
        [Column("Status")]
        public int Status { get; set; }

        /// <summary>
        /// Details of the request (fields to rectify, reason, etc).
        /// </summary>
        [MaxLength(2000)]
        [Column("RequestDetails")]
        public string? RequestDetails { get; set; }

        /// <summary>
        /// For rectification: proposed new values (JSON format).
        /// </summary>
        [Column("ProposedChanges")]
        public string? ProposedChanges { get; set; }

        /// <summary>
        /// Timestamp when request was created.
        /// </summary>
        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when request was processed.
        /// </summary>
        [Column("ProcessedAt")]
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Admin who processed the request.
        /// </summary>
        [MaxLength(255)]
        [Column("ProcessedBy")]
        public string? ProcessedBy { get; set; }

        /// <summary>
        /// Admin's response or notes.
        /// </summary>
        [MaxLength(2000)]
        [Column("AdminResponse")]
        public string? AdminResponse { get; set; }

        /// <summary>
        /// For access requests: path to exported file.
        /// </summary>
        [MaxLength(500)]
        [Column("ExportFilePath")]
        public string? ExportFilePath { get; set; }

        /// <summary>
        /// For access requests: format (JSON or PDF).
        /// </summary>
        [MaxLength(10)]
        [Column("ExportFormat")]
        public string? ExportFormat { get; set; }

        public DataRightsRequest()
        {
            CreatedAt = DateTime.UtcNow;
            Status = 0; // Pending
        }
    }
}
