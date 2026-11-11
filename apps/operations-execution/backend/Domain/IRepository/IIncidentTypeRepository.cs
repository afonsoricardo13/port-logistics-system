using Domain.Model;

namespace Domain.IRepository;

public interface IIncidentTypeRepository
{
    Task<IncidentType?> GetByCodeAsync(string code);
    Task<IEnumerable<IncidentType>> GetAllAsync();
    Task<IEnumerable<IncidentType>> GetRootTypesAsync();
    Task<IEnumerable<IncidentType>> GetChildTypesAsync(string parentCode);
    Task<IncidentType> AddAsync(IncidentType incidentType);
    Task<IncidentType> UpdateAsync(IncidentType incidentType);
    Task<bool> DeleteAsync(string code);
    Task<bool> ExistsAsync(string code);
}
