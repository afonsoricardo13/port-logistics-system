namespace DataModel.Repository;

using DataModel.Mapper;
using DataModel.Model;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

public class StorageAreaRepository : GenericRepository<StorageArea>, IStorageAreaRepository
{
    private readonly StorageAreaMapper _mapper;

    public StorageAreaRepository(AbsanteeContext context, StorageAreaMapper mapper) : base(context)
    {
        _mapper = mapper;
    }

    public new async Task<StorageArea> Add(StorageArea area)
    {
        var dm = _mapper.ToDataModel(area);
        var entry = _context.Set<StorageAreaDataModel>().Add(dm);
        await _context.SaveChangesAsync();
        return _mapper.ToDomain(entry.Entity);
    }

    public async Task<bool> Exists(string identifier)
        => await _context.Set<StorageAreaDataModel>().AnyAsync(s => s.Identifier == identifier);

    public async Task<IEnumerable<StorageArea>> GetAllAsync()
    {
        var list = await _context.Set<StorageAreaDataModel>()
            .Include(s => s.ServedDocks)
            .ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<StorageArea?> GetByIdentifierAsync(string identifier)
    {
        var dm = await _context.Set<StorageAreaDataModel>()
            .Include(s => s.ServedDocks)
            .FirstOrDefaultAsync(s => s.Identifier == identifier);
        return dm == null ? null : _mapper.ToDomain(dm);
    }

    public async Task<StorageArea?> Update(StorageArea area, List<string> errorMessages)
    {
        try
        {
            var dm = await _context.Set<StorageAreaDataModel>()
                .Include(s => s.ServedDocks)
                .FirstAsync(s => s.Identifier == area.Identifier);
            _mapper.UpdateDataModel(dm, area);
            _context.Entry(dm).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return area;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await Exists(area.Identifier))
            {
                errorMessages.Add("Not found");
                return null;
            }
            else throw;
        }
    }
}
