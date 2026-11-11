namespace Application.Services;

using Domain.Model;
using Application.DTO;
using Domain.IRepository;

// US 2.2.13: Qualification management service
public class QualificationService
{
    private readonly IQualificationRepository _qualificationRepository;

    public QualificationService(IQualificationRepository qualificationRepository)
    {
        _qualificationRepository = qualificationRepository;
    }

    public async Task<IEnumerable<QualificationDTO>> GetAllAsync()
    {
        List<Qualification> qualifications = await _qualificationRepository.GetAllAsync();
        return QualificationDTO.ToDTO(qualifications);
    }

    public async Task<QualificationDTO?> GetByQualificationNameAsync(string qualificationName)
    {
        Qualification? qualification = await _qualificationRepository.GetByQualificationNameAsync(qualificationName);
        
        if (qualification != null)
        {
            return QualificationDTO.ToDTO(qualification);
        }
        return null;
    }

    public async Task<IEnumerable<QualificationDTO>> SearchAsync(string? namePattern = null)
    {
        List<Qualification> qualifications = await _qualificationRepository.SearchAsync(namePattern);
        return QualificationDTO.ToDTO(qualifications);
    }

    // BR1: unique name, BR2: 2-100 chars, BR3: requiredNumber >= 0
    public async Task<QualificationDTO?> AddAsync(CreateQualificationDTO qualificationDTO, List<string> errorMessages)
    {
        // Validate qualification name
        if (string.IsNullOrWhiteSpace(qualificationDTO.QualificationName))
        {
            errorMessages.Add("Qualification name cannot be empty");
            return null;
        }

        var trimmedName = qualificationDTO.QualificationName.Trim();

        if (trimmedName.Length < 2)
        {
            errorMessages.Add("Qualification name must be at least 2 characters long");
            return null;
        }

        if (trimmedName.Length > 100)
        {
            errorMessages.Add("Qualification name cannot exceed 100 characters");
            return null;
        }

        if (qualificationDTO.RequiredNumber < 0)
        {
            errorMessages.Add("Required number must be non-negative");
            return null;
        }

        if (qualificationDTO.RequiredNumber > 999)
        {
            errorMessages.Add("Required number cannot exceed 999");
            return null;
        }

        bool exists = await _qualificationRepository.ExistsByQualificationNameAsync(trimmedName);
        if (exists)
        {
            errorMessages.Add($"Qualification with name '{trimmedName}' already exists");
            return null;
        }

        try
        {
            Qualification qualification = QualificationDTO.ToDomain(qualificationDTO);
            Qualification savedQualification = await _qualificationRepository.AddAsync(qualification);
            return QualificationDTO.ToDTO(savedQualification);
        }
        catch (ArgumentException ex)
        {
            errorMessages.Add(ex.Message);
            return null;
        }
    }

    // Only requiredNumber can be updated (name is PK)
    public async Task<QualificationDTO?> UpdateAsync(string qualificationName, UpdateQualificationDTO qualificationDTO, List<string> errorMessages)
    {
        if (qualificationDTO.RequiredNumber < 0)
        {
            errorMessages.Add("Required number must be non-negative");
            return null;
        }

        if (qualificationDTO.RequiredNumber > 999)
        {
            errorMessages.Add("Required number cannot exceed 999");
            return null;
        }

        Qualification? qualification = await _qualificationRepository.GetByQualificationNameAsync(qualificationName);

        if (qualification == null)
        {
            errorMessages.Add($"Qualification with name '{qualificationName}' not found");
            return null;
        }

        try
        {
            QualificationDTO.UpdateToDomain(qualification, qualificationDTO);
            Qualification? updatedQualification = await _qualificationRepository.UpdateAsync(qualification);

            if (updatedQualification == null)
            {
                errorMessages.Add("Failed to update qualification");
                return null;
            }

            return QualificationDTO.ToDTO(updatedQualification);
        }
        catch (ArgumentException ex)
        {
            errorMessages.Add(ex.Message);
            return null;
        }
    }

    public virtual async Task<bool> ExistsByQualificationNameAsync(string qualificationName)
    {
        return await _qualificationRepository.ExistsByQualificationNameAsync(qualificationName);
    }
}
