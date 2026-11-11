namespace Application.DTO;

/// <summary>
/// US 4.1.14: DTO for creating a new Complementary Task Category
/// </summary>
public class CreateComplementaryTaskCategoryDTO
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? DefaultDuration { get; set; }
    public string? ExpectedImpact { get; set; }
}

/// <summary>
/// US 4.1.14: DTO for Complementary Task Category response
/// </summary>
public class ComplementaryTaskCategoryDTO
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? DefaultDuration { get; set; }
    public string? ExpectedImpact { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// US 4.1.14: DTO for updating a Complementary Task Category
/// </summary>
public class UpdateComplementaryTaskCategoryDTO
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? DefaultDuration { get; set; }
    public string? ExpectedImpact { get; set; }
}
