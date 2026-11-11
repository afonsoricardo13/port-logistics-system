namespace Application.Services;

using Application.DTO;
using Domain.IRepository;
using Domain.Model;

public class PhysicalResourceService
{
    private readonly IPhysicalResourceRepository _resourceRepository;

    public PhysicalResourceService(IPhysicalResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    public async Task<IEnumerable<PhysicalResourceDTO>> GetAll()
    {
        var list = await _resourceRepository.GetAllAsync();
        return PhysicalResourceDTO.ToDTO(list);
    }

    public async Task<PhysicalResourceDTO?> GetByCode(string code)
    {
        var r = await _resourceRepository.GetByCodeAsync(code);
        return r == null ? null : PhysicalResourceDTO.ToDTO(r);
    }

    public async Task<IEnumerable<PhysicalResourceDTO>> Search(string? code, string? description, string? type, string? status)
    {
        var list = await _resourceRepository.SearchAsync(code, description, type, status);
        return PhysicalResourceDTO.ToDTO(list);
    }

    public async Task<PhysicalResourceDTO?> Add(PhysicalResourceDTO dto, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            errors.Add("Code is required");
            return null;
        }

        if (await _resourceRepository.Exists(dto.Code))
        {
            errors.Add("Physical resource with this code already exists");
            return null;
        }

        var domain = PhysicalResourceDTO.ToDomain(dto);

        if (domain.Capacity < 0)
        {
            errors.Add("Capacity cannot be negative");
            return null;
        }

        if (domain.SetupTimeMinutes.HasValue && domain.SetupTimeMinutes < 0)
        {
            errors.Add("SetupTimeMinutes cannot be negative");
            return null;
        }

        var saved = await _resourceRepository.Add(domain);
        return PhysicalResourceDTO.ToDTO(saved);
    }

    public async Task<bool> Update(string code, PhysicalResourceDTO dto, List<string> errors)
    {
        var r = await _resourceRepository.GetByCodeAsync(code);
        if (r == null)
        {
            errors.Add("Physical resource not found");
            return false;
        }

        PhysicalResourceDTO.UpdateToDomain(r, dto);

        var updated = await _resourceRepository.Update(r, errors);
        return updated != null;
    }

    public async Task<bool> Deactivate(string code, string? changedBy, List<string> errors)
    {
        var r = await _resourceRepository.GetByCodeAsync(code);
        if (r == null)
        {
            errors.Add("Physical resource not found");
            return false;
        }

        r.Deactivate(changedBy);

        var updated = await _resourceRepository.Update(r, errors);
        return updated != null;
    }

    public async Task<bool> Reactivate(string code, string? changedBy, List<string> errors)
    {
        var r = await _resourceRepository.GetByCodeAsync(code);
        if (r == null)
        {
            errors.Add("Physical resource not found");
            return false;
        }

        r.Reactivate(changedBy);

        var updated = await _resourceRepository.Update(r, errors);
        return updated != null;
    }
}
