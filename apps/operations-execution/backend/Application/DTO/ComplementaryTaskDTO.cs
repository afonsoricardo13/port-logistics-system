namespace Application.DTO;

/// <summary>
/// DTO for creating a new complementary task
/// </summary>
public class CreateComplementaryTaskDTO
{
    public string CategoryCode { get; set; } = string.Empty;
    public string VveId { get; set; } = string.Empty;
    public string ResponsibleTeam { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string ImpactType { get; set; } = string.Empty; // "Parallel" or "Blocking"
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing complementary task
/// </summary>
public class UpdateComplementaryTaskDTO
{
    public string? CategoryCode { get; set; }
    public string? ResponsibleTeam { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Status { get; set; } // "Ongoing" or "Completed"
    public string? ImpactType { get; set; } // "Parallel" or "Blocking"
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for complementary task response
/// </summary>
public class ComplementaryTaskDTO
{
    public Guid Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string VveId { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string ResponsibleTeam { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ImpactType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsOngoing => Status == "Ongoing";
    public int? DurationMinutes => EndTime.HasValue 
        ? (int)(EndTime.Value - StartTime).TotalMinutes 
        : null;
}

/// <summary>
/// DTO for filtering complementary tasks
/// </summary>
public class ComplementaryTaskFilterDTO
{
    public string? Status { get; set; }
    public string? VveId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? OngoingOnly { get; set; }
    public string? ImpactType { get; set; }
}
