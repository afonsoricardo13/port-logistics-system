using System;
using System.Collections.Generic;

namespace Application.DTO
{
    /// <summary>
    /// DTO for creating a data access request (export data).
    /// US 4.5.3 - Request copy of personal data
    /// </summary>
    public class CreateDataAccessRequestDTO
    {
        public string PreferredFormat { get; set; } = "json"; // "json" or "pdf"
    }

    /// <summary>
    /// DTO for creating a data rectification request.
    /// US 4.5.3 - Request correction of data
    /// </summary>
    public class CreateDataRectificationRequestDTO
    {
        public Dictionary<string, string> FieldsToRectify { get; set; } = new();
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for creating a data deletion request.
    /// US 4.5.3 - Request deletion of data
    /// </summary>
    public class CreateDataDeletionRequestDTO
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for processing a data rights request (admin).
    /// </summary>
    public class ProcessDataRightsRequestDTO
    {
        public int RequestId { get; set; }
        public int NewStatus { get; set; } // 1=InProgress, 2=Completed, 3=Rejected
        public string? AdminResponse { get; set; }
    }

    /// <summary>
    /// DTO for returning data rights request information.
    /// </summary>
    public class DataRightsRequestDTO
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int RequestType { get; set; } // 0=Access, 1=Rectification, 2=Deletion
        public string RequestTypeDisplay => RequestType switch
        {
            0 => "Access",
            1 => "Rectification",
            2 => "Deletion",
            _ => "Unknown"
        };
        public int Status { get; set; } // 0=Pending, 1=InProgress, 2=Completed, 3=Rejected
        public string StatusDisplay => Status switch
        {
            0 => "Pending",
            1 => "In Progress",
            2 => "Completed",
            3 => "Rejected",
            _ => "Unknown"
        };
        public string? RequestDetails { get; set; }
        public string? ProposedChanges { get; set; }
        public string? ExportFormat { get; set; }
        public string? ExportFilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedBy { get; set; }
        public string? AdminResponse { get; set; }
    }

    /// <summary>
    /// DTO for user data export (GDPR data portability).
    /// </summary>
    public class UserDataExportDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? Organization { get; set; }
        public string? ContactInfo { get; set; }
        public DateTime ExportGeneratedAt { get; set; } = DateTime.UtcNow;
        public string ExportFormat { get; set; } = string.Empty;
        public List<DataRightsRequestDTO> DataRightsRequestHistory { get; set; } = new();
    }
}
