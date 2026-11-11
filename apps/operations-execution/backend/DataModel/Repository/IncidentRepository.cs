using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository;

public class IncidentRepository : IIncidentRepository
{
    private readonly OEMDbContext _context;

    public IncidentRepository(OEMDbContext context)
    {
        _context = context;
    }

    public async Task<Incident?> GetByIdAsync(Guid id)
    {
        return await _context.Incidents
            .Include(i => i.IncidentType)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Incident>> GetAllAsync()
    {
        return await _context.Incidents
            .Include(i => i.IncidentType)
            .OrderByDescending(i => i.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Incident>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Incidents
            .Include(i => i.IncidentType)
            .Where(i => i.StartTime >= startDate && i.StartTime <= endDate)
            .OrderBy(i => i.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Incident>> GetBySeverityAsync(IncidentSeverity severity)
    {
        return await _context.Incidents
            .Include(i => i.IncidentType)
            .Where(i => i.Severity == severity)
            .OrderByDescending(i => i.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Incident>> GetByStatusAsync(IncidentStatus status)
    {
        return await _context.Incidents
            .Include(i => i.IncidentType)
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Incident>> GetActiveIncidentsAsync()
    {
        return await GetByStatusAsync(IncidentStatus.Active);
    }

    public async Task<Incident> AddAsync(Incident incident)
    {
        await _context.Incidents.AddAsync(incident);
        await _context.SaveChangesAsync();
        return incident;
    }

    public async Task<Incident> UpdateAsync(Incident incident)
    {
        // Calculate duration if incident is resolved
        if (incident.Status == IncidentStatus.Resolved && incident.EndTime.HasValue)
        {
            incident.DurationMinutes = (int)(incident.EndTime.Value - incident.StartTime).TotalMinutes;
            incident.ResolvedAt = DateTime.UtcNow;
        }

        _context.Incidents.Update(incident);
        await _context.SaveChangesAsync();
        return incident;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var incident = await GetByIdAsync(id);
        if (incident == null)
            return false;

        _context.Incidents.Remove(incident);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Incidents.AnyAsync(i => i.Id == id);
    }
}
