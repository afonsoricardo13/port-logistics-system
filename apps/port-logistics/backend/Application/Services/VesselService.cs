namespace Application.Services;

using Application.DTO;
using Domain.IRepository;
using Domain.Model;

public class VesselService
{
    private readonly IVesselRepository _vesselRepository;

    public VesselService(IVesselRepository vesselRepository)
    {
        _vesselRepository = vesselRepository;
    }

    public async Task<IEnumerable<VesselDTO>> GetAll()
    {
        var vessels = await _vesselRepository.GetAllAsync();
        return VesselDTO.ToDTO(vessels);
    }

    public async Task<VesselDTO?> GetByImoNumber(string imoNumber)
    {
        var v = await _vesselRepository.GetByImoNumberAsync(imoNumber);
        return v == null ? null : VesselDTO.ToDTO(v);
    }

    public async Task<IEnumerable<VesselDTO>> Search(string? imoNumber, string? name, string? operatorName)
    {
        var vessels = await _vesselRepository.SearchAsync(imoNumber, name, operatorName);
        return VesselDTO.ToDTO(vessels);
    }

    public async Task<VesselDTO?> Add(VesselDTO dto, List<string> errors)
    {
        // Validate IMO number format
        if (!Vessel.IsValidImoNumber(dto.ImoNumber))
        {
            errors.Add("Invalid IMO number format. Must be 7 digits with valid check digit.");
            return null;
        }

        if (await _vesselRepository.Exists(dto.ImoNumber))
        {
            errors.Add("Vessel with this IMO number already exists");
            return null;
        }

        Vessel v = VesselDTO.ToDomain(dto);
        var saved = await _vesselRepository.Add(v);
        return VesselDTO.ToDTO(saved);
    }

    public async Task<bool> Update(string imoNumber, VesselDTO dto, List<string> errors)
    {
        var v = await _vesselRepository.GetByImoNumberAsync(imoNumber);
        if (v == null)
        {
            errors.Add("Vessel not found");
            return false;
        }

        VesselDTO.UpdateToDomain(v, dto);

        var updated = await _vesselRepository.Update(v, errors);
        return updated != null;
    }
}
