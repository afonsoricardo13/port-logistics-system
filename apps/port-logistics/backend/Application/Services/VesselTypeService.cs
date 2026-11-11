namespace Application.Services;

using Application.DTO;
using Domain.IRepository;
using Domain.Model;

public class VesselTypeService
{
    private readonly IVesselTypeRepository _repository;

    public VesselTypeService(IVesselTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<VesselTypeDTO>> GetAll()
    {
        var list = await _repository.GetAllAsync();
        return VesselTypeDTO.ToDTO(list);
    }

    public async Task<IEnumerable<VesselTypeDTO>> Search(string? name, string? description)
    {
        var list = await _repository.SearchAsync(name, description);
        return VesselTypeDTO.ToDTO(list);
    }

    public async Task<VesselTypeDTO?> GetByName(string name)
    {
        var vt = await _repository.GetByNameAsync(name);
        return vt == null ? null : VesselTypeDTO.ToDTO(vt);
    }

    public async Task<VesselTypeDTO?> Add(VesselTypeDTO dto, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            errors.Add("Name is required");
            return null;
        }
        if (await _repository.Exists(dto.Name))
        {
            errors.Add("A Vessel Type with this name already exists");
            return null;
        }

        try
        {
            VesselType vt = VesselTypeDTO.ToDomain(dto);
            var saved = await _repository.Add(vt);
            return VesselTypeDTO.ToDTO(saved);
        }
        catch (ArgumentException ex)
        {
            errors.Add(ex.Message);
            return null;
        }
    }

    public async Task<bool> Update(string name, VesselTypeDTO dto, List<string> errors)
    {
        var existing = await _repository.GetByNameAsync(name);
        if (existing == null)
        {
            errors.Add("Vessel Type not found");
            return false;
        }

        try
        {
            VesselTypeDTO.UpdateToDomain(existing, dto);
            var updated = await _repository.Update(existing, errors);
            return updated != null;
        }
        catch (ArgumentException ex)
        {
            errors.Add(ex.Message);
            return false;
        }
    }
}
