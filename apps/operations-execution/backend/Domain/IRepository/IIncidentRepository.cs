using Domain.Model;

namespace Domain.IRepository;

public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(Guid id);
    Task<IEnumerable<Incident>> GetAllAsync();
    Task<IEnumerable<Incident>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Incident>> GetBySeverityAsync(IncidentSeverity severity);
    Task<IEnumerable<Incident>> GetByStatusAsync(IncidentStatus status);
    Task<IEnumerable<Incident>> GetActiveIncidentsAsync();
    Task<Incident> AddAsync(Incident incident);
    Task<Incident> UpdateAsync(Incident incident);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
