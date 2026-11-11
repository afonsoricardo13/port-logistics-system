namespace Application.Services;

using Application.DTO;
using Domain.IRepository;
using Domain.Model;

public class DockService
{
    private readonly IDockRepository _dockRepository;

    public DockService(IDockRepository dockRepository)
    {
        _dockRepository = dockRepository;
    }

    public async Task<IEnumerable<DockDTO>> GetAll()
    {
        var docks = await _dockRepository.GetAllAsync();
        return DockDTO.ToDTO(docks);
    }

    public async Task<DockDTO?> GetByCode(string code)
    {
        var d = await _dockRepository.GetByCodeAsync(code);
        return d == null ? null : DockDTO.ToDTO(d);
    }

    public async Task<IEnumerable<DockDTO>> Search(string? name, string? vesselType, string? location)
    {
        var docks = await _dockRepository.SearchAsync(name, vesselType, location);
        return DockDTO.ToDTO(docks);
    }

    public async Task<DockDTO?> Add(DockDTO dto, List<string> errors)
    {
        if (await _dockRepository.Exists(dto.Code))
        {
            errors.Add("Already exists");
            return null;
        }
        Dock d = DockDTO.ToDomain(dto);
        var saved = await _dockRepository.Add(d);
        return DockDTO.ToDTO(saved);
    }

    public async Task<bool> Update(string code, DockDTO dto, List<string> errors)
    {
        var d = await _dockRepository.GetByCodeAsync(code);
        if (d == null)
        {
            errors.Add("Not found");
            return false;
        }

        DockDTO.UpdateToDomain(d, dto);

        var updated = await _dockRepository.Update(d, errors);
        return updated != null;
    }
}
