namespace DataModel.Repository;

using DataModel.Mapper;
using DataModel.Model;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

public class DockRepository : GenericRepository<Dock>, IDockRepository
{
    private readonly DockMapper _mapper;

    public DockRepository(AbsanteeContext context, DockMapper mapper) : base(context)
    {
        _mapper = mapper;
    }

    public new async Task<Dock> Add(Dock dock)
    {
        var dm = _mapper.ToDataModel(dock);
        var entry = _context.Set<DockDataModel>().Add(dm);
        await _context.SaveChangesAsync();
        return _mapper.ToDomain(entry.Entity);
    }

    public async Task<bool> Exists(string code)
        => await _context.Set<DockDataModel>().AnyAsync(d => d.Code == code);

    public async Task<IEnumerable<Dock>> GetAllAsync()
    {
        var list = await _context.Set<DockDataModel>().ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<Dock?> GetByCodeAsync(string code)
    {
        var dm = await _context.Set<DockDataModel>().FirstOrDefaultAsync(d => d.Code == code);
        return dm == null ? null : _mapper.ToDomain(dm);
    }

    public async Task<IEnumerable<Dock>> SearchAsync(string? name, string? vesselType, string? location)
    {
        var query = _context.Set<DockDataModel>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var n = name.ToLower();
            query = query.Where(d => EF.Functions.Like(d.NameOrNumber.ToLower(), $"%{n}%"));
        }
        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.ToLower();
            query = query.Where(d => EF.Functions.Like(d.Location.ToLower(), $"%{loc}%"));
        }
        if (!string.IsNullOrWhiteSpace(vesselType))
        {
            var vt = vesselType.ToLower();
            // Match exact token in CSV (case-insensitive): ",...," LIKE "%,{vt},%"
            query = query.Where(d => EF.Functions.Like(("," + d.AllowedVesselTypesCsv.ToLower() + ","), $"%,{vt},%"));
        }
        var list = await query.ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<Dock?> Update(Dock dock, List<string> errorMessages)
    {
        try
        {
            var dm = await _context.Set<DockDataModel>().FirstAsync(d => d.Code == dock.Code);
            _mapper.UpdateDataModel(dm, dock);
            _context.Entry(dm).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return dock;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await Exists(dock.Code))
            {
                errorMessages.Add("Not found");
                return null;
            }
            else throw;
        }
    }
}
