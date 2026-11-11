using Domain.Model;

namespace Domain.IRepository;

public interface IVesselVisitExecutionRepository
{
    Task<VesselVisitExecution?> GetByIdAsync(string id);
    Task<IEnumerable<VesselVisitExecution>> GetAllAsync();
    Task<IEnumerable<VesselVisitExecution>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<VesselVisitExecution>> GetByVvnIdAsync(string vvnId);
    Task<IEnumerable<VesselVisitExecution>> GetByStatusAsync(VesselVisitExecutionStatus status);
    Task<VesselVisitExecution> AddAsync(VesselVisitExecution vve);
    Task<VesselVisitExecution> UpdateAsync(VesselVisitExecution vve);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<string> GenerateNextIdAsync();
}
