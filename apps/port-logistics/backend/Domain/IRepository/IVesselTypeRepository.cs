namespace Domain.IRepository;

using Domain.Model;

public interface IVesselTypeRepository : IGenericRepository<VesselType>
{
    Task<IEnumerable<VesselType>> GetAllAsync();
    Task<VesselType?> GetByNameAsync(string name);
    new Task<VesselType> Add(VesselType vesselType);
    Task<VesselType?> Update(VesselType vesselType, List<string> errorMessages);
    Task<bool> Exists(string name);
    Task<IEnumerable<VesselType>> SearchAsync(string? name, string? description);
}
