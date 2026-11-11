using Domain.Model;

namespace Domain.IRepository;

public interface IOperationPlanRepository
{
    Task<OperationPlan?> GetByIdAsync(Guid id);
    Task<IEnumerable<OperationPlan>> GetAllAsync();
    Task<IEnumerable<OperationPlan>> GetByDateAsync(DateTime date);
    Task<IEnumerable<OperationPlan>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<OperationPlan>> GetByVvnIdAsync(string vvnId);
    Task<OperationPlan> AddAsync(OperationPlan operationPlan);
    Task<OperationPlan> UpdateAsync(OperationPlan operationPlan);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
