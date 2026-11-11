namespace Application.DTO;

using Domain.Model;

/// <summary>
/// DTO for creating new Qualification
/// US 2.2.13: Register qualifications - requires qualificationName (ID) and requiredNumber
/// </summary>
public class CreateQualificationDTO
{
    public string QualificationName { get; set; } = string.Empty;
    public int RequiredNumber { get; set; }
}

/// <summary>
/// DTO for updating Qualification
/// US 2.2.13: Manage qualifications - only requiredNumber can be updated (qualificationName is the ID and is immutable)
/// </summary>
public class UpdateQualificationDTO
{
    public int RequiredNumber { get; set; }
}

/// <summary>
/// Main DTO for Qualification
/// Used for data transfer between layers
/// </summary>
public class QualificationDTO
{
    public string QualificationName { get; set; } = string.Empty;
    public int RequiredNumber { get; set; }

    public QualificationDTO()
    {
    }

    public QualificationDTO(string qualificationName, int requiredNumber)
    {
        QualificationName = qualificationName;
        RequiredNumber = requiredNumber;
    }

    /// <summary>
    /// Convert domain model to DTO
    /// </summary>
    public static QualificationDTO ToDTO(Qualification qualification)
    {
        return new QualificationDTO(
            qualification.QualificationName,
            qualification.RequiredNumber
        );
    }

    /// <summary>
    /// Convert list of domain models to DTOs
    /// </summary>
    public static IEnumerable<QualificationDTO> ToDTO(IEnumerable<Qualification> qualifications)
    {
        return qualifications.Select(ToDTO).ToList();
    }

    /// <summary>
    /// Convert CreateDTO to domain model
    /// </summary>
    public static Qualification ToDomain(CreateQualificationDTO dto)
    {
        return new Qualification(
            dto.QualificationName,
            dto.RequiredNumber
        );
    }

    /// <summary>
    /// Update domain object from UpdateDTO
    /// Note: QualificationName cannot be changed as it's the identifier
    /// </summary>
    public static void UpdateToDomain(Qualification qualification, UpdateQualificationDTO dto)
    {
        qualification.UpdateRequiredNumber(dto.RequiredNumber);
    }
}
