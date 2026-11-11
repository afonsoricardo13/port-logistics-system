using Domain.Model;

namespace Domain.IRepository;

public interface IComplementaryTaskCategoryRepository
{
    Task<ComplementaryTaskCategory?> GetByCodeAsync(string code);
    Task<IEnumerable<ComplementaryTaskCategory>> GetAllAsync();
    Task<ComplementaryTaskCategory> AddAsync(ComplementaryTaskCategory category);
    Task<ComplementaryTaskCategory> UpdateAsync(ComplementaryTaskCategory category);
    Task<bool> DeleteAsync(string code);
    Task<bool> ExistsAsync(string code);
}
