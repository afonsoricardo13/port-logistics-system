namespace DataModel.Repository;

using DataModel.Mapper;
using DataModel.Model;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

public class VesselVisitNotificationRepository : GenericRepository<VesselVisitNotification>, IVesselVisitNotificationRepository
{
    private readonly VesselVisitNotificationMapper _mapper;

    public VesselVisitNotificationRepository(AbsanteeContext context, VesselVisitNotificationMapper mapper) : base(context)
    {
        _mapper = mapper;
    }

    public new async Task<VesselVisitNotification> Add(VesselVisitNotification vvn)
    {
        var dm = _mapper.ToDataModel(vvn);
        var entry = _context.Set<VesselVisitNotificationDataModel>().Add(dm);
        await _context.SaveChangesAsync();
        return _mapper.ToDomain(entry.Entity);
    }

    public new IEnumerable<VesselVisitNotification> GetAll()
    {
        var list = _context.Set<VesselVisitNotificationDataModel>()
            .Include(x => x.CargoManifest)
            .Include(x => x.Crew)
            .ToList();
        return _mapper.ToDomain(list);
    }

    public async Task<IEnumerable<VesselVisitNotification>> GetAllAsync()
    {
        var list = await _context.Set<VesselVisitNotificationDataModel>()
            .Include(x => x.CargoManifest)
            .Include(x => x.Crew)
            .ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<VesselVisitNotification?> GetByIdAsync(long id)
    {
        var dm = await _context.Set<VesselVisitNotificationDataModel>()
            .Include(x => x.CargoManifest)
            .Include(x => x.Crew)
            .FirstOrDefaultAsync(x => x.Id == id);
        return dm == null ? null : _mapper.ToDomain(dm);
    }

    public async Task<IEnumerable<VesselVisitNotification>> GetByStatusAsync(VvnStatus status)
    {
        var list = await _context.Set<VesselVisitNotificationDataModel>()
            .Include(x => x.CargoManifest)
            .Include(x => x.Crew)
            .Where(x => x.Status == (int)status)
            .ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<VesselVisitNotification?> Update(VesselVisitNotification vvn, List<string> errorMessages)
    {
        try
        {
            var dm = await _context.Set<VesselVisitNotificationDataModel>()
                .Include(x => x.CargoManifest)
                .Include(x => x.Crew)
                .FirstAsync(x => x.Id == vvn.Id);
            _mapper.UpdateDataModel(dm, vvn);
            _context.Entry(dm).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return vvn;
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _context.Set<VesselVisitNotificationDataModel>().AnyAsync(x => x.Id == vvn.Id);
            if (!exists)
            {
                errorMessages.Add("Not found");
                return null;
            }
            else throw;
        }
    }

    public async Task<IEnumerable<VesselVisitNotification>> GetFilteredAsync(
        string? vesselImoNumber = null,
        VvnStatus? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        long? representativeId = null,
        long? organizationId = null)
    {
        var query = _context.Set<VesselVisitNotificationDataModel>()
            .Include(x => x.CargoManifest)
            .Include(x => x.Crew)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(vesselImoNumber))
            query = query.Where(x => x.VesselImoNumber.Contains(vesselImoNumber));

        if (status.HasValue)
            query = query.Where(x => x.Status == (int)status.Value);

        if (dateFrom.HasValue)
            query = query.Where(x => x.EstimatedTimeOfArrival >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.EstimatedTimeOfArrival <= dateTo.Value);

        if (representativeId.HasValue)
            query = query.Where(x => x.SubmittedByRepresentativeId == representativeId.Value);

        if (organizationId.HasValue)
            query = query.Where(x => ((AbsanteeContext)_context).Representatives
                .Any(r => r.Id == x.SubmittedByRepresentativeId && r.OrganizationId == organizationId.Value));

        var list = await query.OrderByDescending(x => x.Id).ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<IEnumerable<VesselVisitNotification>> GetByRepresentativeIdAsync(long representativeId)
    {
        var list = await _context.Set<VesselVisitNotificationDataModel>()
            .Include(x => x.CargoManifest)
            .Include(x => x.Crew)
            .Where(x => x.SubmittedByRepresentativeId == representativeId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<IEnumerable<VesselVisitNotification>> GetByOrganizationIdAsync(long organizationId)
    {
        var list = await _context.Set<VesselVisitNotificationDataModel>()
            .Include(x => x.CargoManifest)
            .Include(x => x.Crew)
            .Where(x => ((AbsanteeContext)_context).Representatives
                .Any(r => r.Id == x.SubmittedByRepresentativeId && r.OrganizationId == organizationId))
            .OrderByDescending(x => x.Id)
            .ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<bool> Delete(long id)
    {
        var dm = await _context.Set<VesselVisitNotificationDataModel>()
            .Include(x => x.CargoManifest)
            .Include(x => x.Crew)
            .FirstOrDefaultAsync(x => x.Id == id);
        
        if (dm == null) return false;
        
        _context.Set<VesselVisitNotificationDataModel>().Remove(dm);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task UpdateAdditionalFields(long vvnId, string? captainName, int? crewCount, 
        List<string>? safetyOfficers, string? preferredDockCode, 
        object? unloadingManifest, object? loadingManifest)
    {
        var dm = await _context.Set<VesselVisitNotificationDataModel>()
            .FirstOrDefaultAsync(x => x.Id == vvnId);
        
        if (dm != null)
        {
            dm.CaptainName = captainName;
            dm.CrewCount = crewCount;
            dm.SafetyOfficersJson = safetyOfficers != null ? System.Text.Json.JsonSerializer.Serialize(safetyOfficers) : null;
            dm.PreferredDockCode = preferredDockCode;
            dm.UnloadingManifestJson = unloadingManifest != null ? System.Text.Json.JsonSerializer.Serialize(unloadingManifest) : null;
            dm.LoadingManifestJson = loadingManifest != null ? System.Text.Json.JsonSerializer.Serialize(loadingManifest) : null;
            
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<(string? CaptainName, int? CrewCount, List<string>? SafetyOfficers, string? PreferredDockCode, object? UnloadingManifest, object? LoadingManifest)> GetAdditionalFieldsAsync(long id)
    {
        var dm = await _context.Set<VesselVisitNotificationDataModel>()
            .FirstOrDefaultAsync(x => x.Id == id);
        
        if (dm == null)
            return (null, null, null, null, null, null);
        
        List<string>? safetyOfficers = null;
        if (!string.IsNullOrWhiteSpace(dm.SafetyOfficersJson))
        {
            safetyOfficers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(dm.SafetyOfficersJson);
        }
        
        object? unloadingManifest = null;
        if (!string.IsNullOrWhiteSpace(dm.UnloadingManifestJson))
        {
            unloadingManifest = System.Text.Json.JsonSerializer.Deserialize<object>(dm.UnloadingManifestJson);
        }
        
        object? loadingManifest = null;
        if (!string.IsNullOrWhiteSpace(dm.LoadingManifestJson))
        {
            loadingManifest = System.Text.Json.JsonSerializer.Deserialize<object>(dm.LoadingManifestJson);
        }
        
        return (dm.CaptainName, dm.CrewCount, safetyOfficers, dm.PreferredDockCode, unloadingManifest, loadingManifest);
    }
}
