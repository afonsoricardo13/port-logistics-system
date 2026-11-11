namespace Application.Services;

using Application.DTO;
using Domain.IRepository;
using Domain.Model;

public class StorageAreaService
{
    private readonly IStorageAreaRepository _repo;

    public StorageAreaService(IStorageAreaRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<StorageAreaDTO>> GetAll()
    {
        var list = await _repo.GetAllAsync();
        return StorageAreaDTO.ToDTO(list);
    }

    public async Task<StorageAreaDTO?> GetByIdentifier(string identifier)
    {
        var s = await _repo.GetByIdentifierAsync(identifier);
        return s == null ? null : StorageAreaDTO.ToDTO(s);
    }

    public async Task<StorageAreaDTO?> Add(StorageAreaDTO dto, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(dto.Identifier)) { errors.Add("Identifier is required"); return null; }
        if (await _repo.Exists(dto.Identifier)) { errors.Add("Storage area with this identifier already exists"); return null; }

        if (dto.MaxCapacityTeu < 0) { errors.Add("Max capacity cannot be negative"); return null; }
        if (dto.CurrentOccupancyTeu < 0) { errors.Add("Current occupancy cannot be negative"); return null; }
        if (dto.CurrentOccupancyTeu > dto.MaxCapacityTeu) { errors.Add("Current occupancy cannot exceed maximum capacity"); return null; }

        // Optional: validate type
        if (!string.Equals(dto.Type, "yard", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(dto.Type, "warehouse", StringComparison.OrdinalIgnoreCase))
        {
            // Allow custom but warn? For now, accept any string; remove strict validation
        }

        var domain = StorageAreaDTO.ToDomain(dto);
        var saved = await _repo.Add(domain);
        return StorageAreaDTO.ToDTO(saved);
    }

    public async Task<StorageAreaDTO?> Update(string identifier, StorageAreaDTO dto, List<string> errors)
    {
        var s = await _repo.GetByIdentifierAsync(identifier);
        if (s == null) { errors.Add("Storage area not found"); return null; }

        try
        {
            StorageAreaDTO.UpdateToDomain(s, dto);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            return null;
        }

        var updated = await _repo.Update(s, errors);
        return updated == null ? null : StorageAreaDTO.ToDTO(updated);
    }
}
