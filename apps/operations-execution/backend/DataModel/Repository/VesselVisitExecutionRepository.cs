using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository;

public class VesselVisitExecutionRepository : IVesselVisitExecutionRepository
{
    private readonly OEMDbContext _context;

    public VesselVisitExecutionRepository(OEMDbContext context)
    {
        _context = context;
    }

    public async Task<VesselVisitExecution?> GetByIdAsync(string id)
    {
        return await _context.VesselVisitExecutions.FindAsync(id);
    }

    public async Task<IEnumerable<VesselVisitExecution>> GetAllAsync()
    {
        return await _context.VesselVisitExecutions
            .OrderByDescending(vve => vve.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<VesselVisitExecution>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.VesselVisitExecutions
            .Where(vve => vve.ActualArrivalTime != null && 
                         vve.ActualArrivalTime >= startDate && 
                         vve.ActualArrivalTime <= endDate)
            .OrderBy(vve => vve.ActualArrivalTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<VesselVisitExecution>> GetByVvnIdAsync(string vvnId)
    {
        return await _context.VesselVisitExecutions
            .Where(vve => vve.VvnId == vvnId)
            .OrderBy(vve => vve.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<VesselVisitExecution>> GetByStatusAsync(VesselVisitExecutionStatus status)
    {
        return await _context.VesselVisitExecutions
            .Where(vve => vve.Status == status)
            .OrderByDescending(vve => vve.CreatedAt)
            .ToListAsync();
    }

    public async Task<VesselVisitExecution> AddAsync(VesselVisitExecution vve)
    {
        await _context.VesselVisitExecutions.AddAsync(vve);
        await _context.SaveChangesAsync();
        return vve;
    }

    public async Task<VesselVisitExecution> UpdateAsync(VesselVisitExecution vve)
    {
        vve.UpdatedAt = DateTime.UtcNow;
        _context.VesselVisitExecutions.Update(vve);
        await _context.SaveChangesAsync();
        return vve;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var vve = await GetByIdAsync(id);
        if (vve == null)
            return false;

        _context.VesselVisitExecutions.Remove(vve);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.VesselVisitExecutions.AnyAsync(vve => vve.Id == id);
    }

    public async Task<string> GenerateNextIdAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"VVE-{year}-";
        
        var lastVve = await _context.VesselVisitExecutions
            .Where(v => v.Id.StartsWith(prefix))
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync();

        if (lastVve == null)
        {
            return $"{prefix}001";
        }

        var lastNumber = int.Parse(lastVve.Id.Substring(prefix.Length));
        var nextNumber = lastNumber + 1;
        
        return $"{prefix}{nextNumber:D3}";
    }
}
