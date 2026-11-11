using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository;

public class ComplementaryTaskRepository : IComplementaryTaskRepository
{
    private readonly OEMDbContext _context;

    public ComplementaryTaskRepository(OEMDbContext context)
    {
        _context = context;
    }

    public async Task<ComplementaryTask?> GetByIdAsync(Guid id)
    {
        return await _context.ComplementaryTasks
            .Include(t => t.Category)
            .Include(t => t.VesselVisitExecution)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<ComplementaryTask>> GetAllAsync()
    {
        return await _context.ComplementaryTasks
            .Include(t => t.Category)
            .Include(t => t.VesselVisitExecution)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<ComplementaryTask>> GetByVveIdAsync(string vveId)
    {
        return await _context.ComplementaryTasks
            .Include(t => t.Category)
            .Where(t => t.VveId == vveId)
            .OrderBy(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<ComplementaryTask>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.ComplementaryTasks
            .Include(t => t.Category)
            .Include(t => t.VesselVisitExecution)
            .Where(t => t.StartTime >= startDate && t.StartTime <= endDate)
            .OrderBy(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<ComplementaryTask>> GetByStatusAsync(ComplementaryTaskStatus status)
    {
        return await _context.ComplementaryTasks
            .Include(t => t.Category)
            .Include(t => t.VesselVisitExecution)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<ComplementaryTask>> GetOngoingTasksAsync()
    {
        return await GetByStatusAsync(ComplementaryTaskStatus.Ongoing);
    }

    public async Task<ComplementaryTask> AddAsync(ComplementaryTask task)
    {
        task.CreatedAt = DateTime.UtcNow;
        await _context.ComplementaryTasks.AddAsync(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<ComplementaryTask> UpdateAsync(ComplementaryTask task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        _context.ComplementaryTasks.Update(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var task = await GetByIdAsync(id);
        if (task == null)
            return false;

        _context.ComplementaryTasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.ComplementaryTasks.AnyAsync(t => t.Id == id);
    }
}
