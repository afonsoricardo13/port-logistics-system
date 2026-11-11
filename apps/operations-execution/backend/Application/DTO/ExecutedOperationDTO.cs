namespace Application.DTO;

/// <summary>
/// US 4.1.9: DTO for Executed Operation response
/// </summary>
public class ExecutedOperationDTO
{
    public Guid Id { get; set; }
    public string VveId { get; set; } = string.Empty;
    public Guid? OperationPlanId { get; set; }
    public int PlannedOperationIndex { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PlannedStartTime { get; set; }
    public DateTime? PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public List<ResourceUsageDTO>? ActualResources { get; set; }
    public int? DelayMinutes { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// US 4.1.9: DTO for creating a new executed operation from a planned operation
/// </summary>
public class CreateExecutedOperationDTO
{
    public required string VveId { get; set; }
    public Guid? OperationPlanId { get; set; }
    public int PlannedOperationIndex { get; set; }
    public required string Type { get; set; } // "loading" or "unloading"
    public DateTime? PlannedStartTime { get; set; }
    public DateTime? PlannedEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public List<ResourceUsageDTO>? ActualResources { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// US 4.1.9: DTO for updating an executed operation
/// </summary>
public class UpdateExecutedOperationDTO
{
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public List<ResourceUsageDTO>? ActualResources { get; set; }
    public string? Status { get; set; } // "Started", "Completed", "Delayed"
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for resource usage in executed operations
/// </summary>
public class ResourceUsageDTO
{
    public string Type { get; set; } = string.Empty; // "crane", "dock", "staff"
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
}

/// <summary>
/// US 4.1.9: Response DTO for bulk operation initialization from operation plan
/// </summary>
public class InitializeOperationsFromPlanResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int OperationsInitialized { get; set; }
    public List<ExecutedOperationDTO> Operations { get; set; } = new();
}
