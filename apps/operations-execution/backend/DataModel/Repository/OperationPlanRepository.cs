using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository;

public class OperationPlanRepository : IOperationPlanRepository
{
    private readonly OEMDbContext _context;

    public OperationPlanRepository(OEMDbContext context)
    {
        _context = context;
    }

    public async Task<OperationPlan?> GetByIdAsync(Guid id)
    {
        return await _context.OperationPlans.FindAsync(id);
    }

    public async Task<IEnumerable<OperationPlan>> GetAllAsync()
    {
        return await _context.OperationPlans.ToListAsync();
    }

    public async Task<IEnumerable<OperationPlan>> GetByDateAsync(DateTime date)
    {
        return await _context.OperationPlans
            .Where(op => op.Date.Date == date.Date)
            .OrderBy(op => op.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<OperationPlan>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.OperationPlans
            .Where(op => op.Date.Date >= startDate.Date && op.Date.Date <= endDate.Date)
            .OrderBy(op => op.Date)
            .ThenBy(op => op.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<OperationPlan>> GetByVvnIdAsync(string vvnId)
    {
        return await _context.OperationPlans
            .Where(op => op.VvnId == vvnId)
            .OrderBy(op => op.CreatedAt)
            .ToListAsync();
    }

    public async Task<OperationPlan> AddAsync(OperationPlan operationPlan)
    {
        await _context.OperationPlans.AddAsync(operationPlan);
        await _context.SaveChangesAsync();
        return operationPlan;
    }

    public async Task<OperationPlan> UpdateAsync(OperationPlan operationPlan)
    {
        _context.OperationPlans.Update(operationPlan);
        await _context.SaveChangesAsync();
        return operationPlan;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var operationPlan = await GetByIdAsync(id);
        if (operationPlan == null)
            return false;

        _context.OperationPlans.Remove(operationPlan);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.OperationPlans.AnyAsync(op => op.Id == id);
    }
}
