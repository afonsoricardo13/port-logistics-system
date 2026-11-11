using Application.DTO;
using Domain.IRepository;
using Domain.Model;

namespace Application.Services;

public class IncidentTypeService
{
    private readonly IIncidentTypeRepository _repository;

    public IncidentTypeService(IIncidentTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<IncidentTypeDTO>> GetAllAsync(bool? rootOnly = null, string? parentCode = null)
    {
        IEnumerable<IncidentType> types;

        if (rootOnly == true)
            types = await _repository.GetRootTypesAsync();
        else if (!string.IsNullOrEmpty(parentCode))
            types = await _repository.GetChildTypesAsync(parentCode);
        else
            types = await _repository.GetAllAsync();

        return types.Select(MapToDTO);
    }

    public async Task<IncidentTypeDTO?> GetByCodeAsync(string code)
    {
        var type = await _repository.GetByCodeAsync(code);
        return type == null ? null : MapToDTO(type);
    }

    public async Task<IncidentTypeDTO> CreateAsync(CreateIncidentTypeDTO dto)
    {
        if (await _repository.ExistsAsync(dto.Code))
            throw new InvalidOperationException($"Incident type with code {dto.Code} already exists");

        if (!string.IsNullOrEmpty(dto.ParentTypeCode) && !await _repository.ExistsAsync(dto.ParentTypeCode))
            throw new InvalidOperationException($"Parent type with code {dto.ParentTypeCode} not found");

        if (!Enum.TryParse<IncidentSeverity>(dto.Severity, true, out var severity))
            throw new ArgumentException($"Invalid severity: {dto.Severity}. Valid values: Minor, Major, Critical");

        var incidentType = new IncidentType
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Severity = severity,
            ParentTypeCode = dto.ParentTypeCode
        };

        var created = await _repository.AddAsync(incidentType);
        return MapToDTO(created);
    }

    public async Task<IncidentTypeDTO> UpdateAsync(string code, UpdateIncidentTypeDTO dto)
    {
        var existing = await _repository.GetByCodeAsync(code);
        if (existing == null)
            throw new KeyNotFoundException($"Incident type with code {code} not found");

        if (!string.IsNullOrEmpty(dto.ParentTypeCode))
        {
            if (dto.ParentTypeCode == code)
                throw new InvalidOperationException("An incident type cannot be its own parent");

            if (!await _repository.ExistsAsync(dto.ParentTypeCode))
                throw new InvalidOperationException($"Parent type with code {dto.ParentTypeCode} not found");
        }

        if (!Enum.TryParse<IncidentSeverity>(dto.Severity, true, out var severity))
            throw new ArgumentException($"Invalid severity: {dto.Severity}. Valid values: Minor, Major, Critical");

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Severity = severity;
        existing.ParentTypeCode = dto.ParentTypeCode;

        var updated = await _repository.UpdateAsync(existing);
        return MapToDTO(updated);
    }

    public async Task<bool> DeleteAsync(string code)
    {
        var existing = await _repository.GetByCodeAsync(code);
        if (existing == null)
            return false;

        if (existing.ChildTypes.Any())
            throw new InvalidOperationException($"Cannot delete incident type {code} because it has child types");

        return await _repository.DeleteAsync(code);
    }

    private static IncidentTypeDTO MapToDTO(IncidentType type)
    {
        return new IncidentTypeDTO
        {
            Code = type.Code,
            Name = type.Name,
            Description = type.Description,
            Severity = type.Severity.ToString(),
            ParentTypeCode = type.ParentTypeCode,
            ParentTypeName = type.ParentType?.Name,
            ChildTypes = type.ChildTypes.Select(MapToDTO).ToList()
        };
    }
}
