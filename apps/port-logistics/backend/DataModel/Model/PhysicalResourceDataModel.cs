namespace DataModel.Model;

using System.ComponentModel.DataAnnotations;
using Domain.Model;

public class PhysicalResourceDataModel
{
    public long Id { get; set; }

    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? AssignedArea { get; set; }
    public string? Status { get; set; }
    public int? SetupTimeMinutes { get; set; }
    // Stored as a comma-separated list for simplicity
    public string? QualificationRequirementIdsCsv { get; set; }

    // Crane-specific fields
    public string? OperationalWindow { get; set; }
    public int? CapacityPerHour { get; set; }
    
    // Truck-specific fields
    public double? AverageSpeedKmH { get; set; }

    // Audit
    public DateTime? ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string? OperationType { get; set; }

    public PhysicalResourceDataModel() { }

    public PhysicalResourceDataModel(PhysicalResource r)
    {
        Code = r.Code;
        Description = r.Description;
        Type = r.Type;
        Capacity = r.Capacity;
        AssignedArea = r.AssignedArea;
        Status = r.Status.ToString();
        SetupTimeMinutes = r.SetupTimeMinutes;
        QualificationRequirementIdsCsv = r.QualificationRequirementIds != null ? string.Join(',', r.QualificationRequirementIds) : null;
        OperationalWindow = r.OperationalWindow;
        CapacityPerHour = r.CapacityPerHour;
        AverageSpeedKmH = r.AverageSpeedKmH;
        ChangedAt = r.ChangedAt;
        ChangedBy = r.ChangedBy;
        OperationType = r.OperationType;
    }
}

