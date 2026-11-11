using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository;

/// <summary>
/// US 4.1.9: Repository implementation for ExecutedOperation entity
/// </summary>
public class ExecutedOperationRepository : IExecutedOperationRepository
{
    private readonly OEMDbContext _context;

    public ExecutedOperationRepository(OEMDbContext context)
    {
        _context = context;
    }

    public async Task<ExecutedOperation?> GetByIdAsync(Guid id)
    {
        return await _context.ExecutedOperations.FindAsync(id);
    }

    public async Task<IEnumerable<ExecutedOperation>> GetByVveIdAsync(string vveId)
    {
        return await _context.ExecutedOperations
            .Where(eo => eo.VveId == vveId)
            .OrderBy(eo => eo.PlannedStartTime ?? eo.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ExecutedOperation>> GetByStatusAsync(ExecutionStatus status)
    {
        return await _context.ExecutedOperations
            .Where(eo => eo.Status == status)
            .OrderByDescending(eo => eo.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ExecutedOperation>> GetByOperationPlanIdAsync(Guid operationPlanId)
    {
        return await _context.ExecutedOperations
            .Where(eo => eo.OperationPlanId == operationPlanId)
            .OrderBy(eo => eo.PlannedOperationIndex)
            .ToListAsync();
    }

    public async Task<ExecutedOperation> AddAsync(ExecutedOperation executedOperation)
    {
        executedOperation.CreatedAt = DateTime.UtcNow;
        executedOperation.UpdatedAt = DateTime.UtcNow;
        
        await _context.ExecutedOperations.AddAsync(executedOperation);
        await _context.SaveChangesAsync();
        return executedOperation;
    }

    public async Task<ExecutedOperation> UpdateAsync(ExecutedOperation executedOperation)
    {
        executedOperation.UpdatedAt = DateTime.UtcNow;
        
        // Calculate delay if both actual and planned end times are set
        if (executedOperation.ActualEndTime.HasValue && executedOperation.PlannedEndTime.HasValue)
        {
            executedOperation.DelayMinutes = (int)(executedOperation.ActualEndTime.Value - executedOperation.PlannedEndTime.Value).TotalMinutes;
        }
        
        _context.ExecutedOperations.Update(executedOperation);
        await _context.SaveChangesAsync();
        return executedOperation;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var executedOperation = await GetByIdAsync(id);
        if (executedOperation == null)
            return false;

        _context.ExecutedOperations.Remove(executedOperation);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.ExecutedOperations.AnyAsync(eo => eo.Id == id);
    }

    public async Task<IEnumerable<ExecutedOperation>> GetAllAsync()
    {
        return await _context.ExecutedOperations
            .OrderByDescending(eo => eo.UpdatedAt)
            .ToListAsync();
    }
}
