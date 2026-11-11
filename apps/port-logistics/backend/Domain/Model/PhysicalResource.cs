namespace Domain.Model;

using System;
using System.Collections.Generic;

public enum PhysicalResourceStatus
{
    Active,
    Inactive,
    UnderMaintenance
}

public class PhysicalResource
{
    public long Id { get; set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public string? AssignedArea { get; private set; }
    public PhysicalResourceStatus Status { get; private set; } = PhysicalResourceStatus.Active;
    public int? SetupTimeMinutes { get; private set; }
    // List of qualification ids (references to system qualifications)
    private List<string> _qualificationRequirementIds = new();
    public IReadOnlyCollection<string> QualificationRequirementIds => _qualificationRequirementIds.AsReadOnly();

    // Crane-specific fields
    public string? OperationalWindow { get; private set; }
    public int? CapacityPerHour { get; private set; }
    
    // Truck-specific fields
    public double? AverageSpeedKmH { get; private set; }

    // Audit fields (simple representation)
    public DateTime? ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string? OperationType { get; set; }

    private PhysicalResource() { }

    public PhysicalResource(string code, string description, string type, int capacity,
        string? assignedArea = null, PhysicalResourceStatus status = PhysicalResourceStatus.Active,
        int? setupTimeMinutes = null, IEnumerable<string>? qualificationRequirementIds = null,
        string? operationalWindow = null, int? capacityPerHour = null, double? averageSpeedKmH = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required");
        if (code.Length > 20) throw new ArgumentException("Code cannot exceed 20 characters");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required");
        if (description.Length > 255) throw new ArgumentException("Description cannot exceed 255 characters");
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required");
        if (capacity < 0) throw new ArgumentException("Capacity cannot be negative");

        Code = code.Trim();
        Description = description.Trim();
        Type = type.Trim();
        Capacity = capacity;
        AssignedArea = string.IsNullOrWhiteSpace(assignedArea) ? null : assignedArea?.Trim();
        Status = status;
        SetupTimeMinutes = setupTimeMinutes;
        if (qualificationRequirementIds != null)
            _qualificationRequirementIds = qualificationRequirementIds.ToList();
        
        OperationalWindow = operationalWindow;
        CapacityPerHour = capacityPerHour;
        AverageSpeedKmH = averageSpeedKmH;
    }

    public void Update(string? description, string? type, int? capacity, string? assignedArea,
        PhysicalResourceStatus? status, int? setupTimeMinutes, IEnumerable<string>? qualificationRequirementIds,
        string? operationalWindow, int? capacityPerHour, double? averageSpeedKmH,
        string? changedBy = null, string? operation = null)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            if (description!.Length > 255) throw new ArgumentException("Description cannot exceed 255 characters");
            Description = description.Trim();
        }
        if (!string.IsNullOrWhiteSpace(type)) Type = type!.Trim();
        if (capacity.HasValue)
        {
            if (capacity.Value < 0) throw new ArgumentException("Capacity cannot be negative");
            Capacity = capacity.Value;
        }
        AssignedArea = string.IsNullOrWhiteSpace(assignedArea) ? AssignedArea : assignedArea?.Trim();
        if (status.HasValue) Status = status.Value;
        if (setupTimeMinutes.HasValue)
        {
            if (setupTimeMinutes.Value < 0) throw new ArgumentException("Setup time cannot be negative");
            SetupTimeMinutes = setupTimeMinutes.Value;
        }
        if (qualificationRequirementIds != null)
        {
            _qualificationRequirementIds = qualificationRequirementIds.ToList();
        }
        
        OperationalWindow = operationalWindow;
        CapacityPerHour = capacityPerHour;
        AverageSpeedKmH = averageSpeedKmH;

        ChangedAt = DateTime.UtcNow;
        ChangedBy = changedBy;
        OperationType = operation;
    }

    public void Deactivate(string? changedBy = null)
    {
        if (Status != PhysicalResourceStatus.Inactive)
        {
            Status = PhysicalResourceStatus.Inactive;
            ChangedAt = DateTime.UtcNow;
            ChangedBy = changedBy;
            OperationType = "Deactivate";
        }
    }

    public void Reactivate(string? changedBy = null)
    {
        if (Status != PhysicalResourceStatus.Active)
        {
            Status = PhysicalResourceStatus.Active;
            ChangedAt = DateTime.UtcNow;
            ChangedBy = changedBy;
            OperationType = "Reactivate";
        }
    }
}

