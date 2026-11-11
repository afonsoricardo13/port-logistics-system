using Domain.Model;

namespace Domain.IRepository;

public interface IComplementaryTaskRepository
{
    Task<ComplementaryTask?> GetByIdAsync(Guid id);
    Task<IEnumerable<ComplementaryTask>> GetAllAsync();
    Task<IEnumerable<ComplementaryTask>> GetByVveIdAsync(string vveId);
    Task<IEnumerable<ComplementaryTask>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<ComplementaryTask>> GetByStatusAsync(ComplementaryTaskStatus status);
    Task<IEnumerable<ComplementaryTask>> GetOngoingTasksAsync();
    Task<ComplementaryTask> AddAsync(ComplementaryTask task);
    Task<ComplementaryTask> UpdateAsync(ComplementaryTask task);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
