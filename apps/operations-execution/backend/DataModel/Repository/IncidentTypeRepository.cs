using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository;

public class IncidentTypeRepository : IIncidentTypeRepository
{
    private readonly OEMDbContext _context;

    public IncidentTypeRepository(OEMDbContext context)
    {
        _context = context;
    }

    public async Task<IncidentType?> GetByCodeAsync(string code)
    {
        return await _context.IncidentTypes
            .Include(it => it.ParentType)
            .Include(it => it.ChildTypes)
            .FirstOrDefaultAsync(it => it.Code == code);
    }

    public async Task<IEnumerable<IncidentType>> GetAllAsync()
    {
        return await _context.IncidentTypes
            .Include(it => it.ParentType)
            .Include(it => it.ChildTypes)
            .OrderBy(it => it.Code)
            .ToListAsync();
    }

    public async Task<IEnumerable<IncidentType>> GetRootTypesAsync()
    {
        return await _context.IncidentTypes
            .Where(it => it.ParentTypeCode == null)
            .Include(it => it.ChildTypes)
            .OrderBy(it => it.Code)
            .ToListAsync();
    }

    public async Task<IEnumerable<IncidentType>> GetChildTypesAsync(string parentCode)
    {
        return await _context.IncidentTypes
            .Where(it => it.ParentTypeCode == parentCode)
            .Include(it => it.ChildTypes)
            .OrderBy(it => it.Code)
            .ToListAsync();
    }

    public async Task<IncidentType> AddAsync(IncidentType incidentType)
    {
        await _context.IncidentTypes.AddAsync(incidentType);
        await _context.SaveChangesAsync();
        return incidentType;
    }

    public async Task<IncidentType> UpdateAsync(IncidentType incidentType)
    {
        _context.IncidentTypes.Update(incidentType);
        await _context.SaveChangesAsync();
        return incidentType;
    }

    public async Task<bool> DeleteAsync(string code)
    {
        var incidentType = await GetByCodeAsync(code);
        if (incidentType == null)
            return false;

        _context.IncidentTypes.Remove(incidentType);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string code)
    {
        return await _context.IncidentTypes.AnyAsync(it => it.Code == code);
    }
}
