namespace DataModel.Repository;

using DataModel.Mapper;
using DataModel.Model;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

public class VesselTypeRepository : GenericRepository<VesselType>, IVesselTypeRepository
{
    private readonly VesselTypeMapper _mapper;

    public VesselTypeRepository(AbsanteeContext context, VesselTypeMapper mapper) : base(context)
    {
        _mapper = mapper;
    }

    public new async Task<VesselType> Add(VesselType vesselType)
    {
        var dm = _mapper.ToDataModel(vesselType);
        var entry = _context.Set<VesselTypeDataModel>().Add(dm);
        await _context.SaveChangesAsync();
        return _mapper.ToDomain(entry.Entity);
    }

    public async Task<bool> Exists(string name)
        => await _context.Set<VesselTypeDataModel>().AnyAsync(v => v.Name == name);

    public async Task<IEnumerable<VesselType>> GetAllAsync()
    {
        var list = await _context.Set<VesselTypeDataModel>().ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<VesselType?> GetByNameAsync(string name)
    {
        var dm = await _context.Set<VesselTypeDataModel>().FirstOrDefaultAsync(v => v.Name == name);
        return dm == null ? null : _mapper.ToDomain(dm);
    }

    public async Task<IEnumerable<VesselType>> SearchAsync(string? name, string? description)
    {
        var query = _context.Set<VesselTypeDataModel>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(v => EF.Functions.Like(v.Name, $"%{name}%"));

        if (!string.IsNullOrWhiteSpace(description))
            query = query.Where(v => EF.Functions.Like(v.Description, $"%{description}%"));

        var list = await query.ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<VesselType?> Update(VesselType vesselType, List<string> errorMessages)
    {
        try
        {
            var dm = await _context.Set<VesselTypeDataModel>().FirstAsync(v => v.Name == vesselType.Name);
            _mapper.UpdateDataModel(dm, vesselType);
            _context.Entry(dm).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return vesselType;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await Exists(vesselType.Name))
            {
                errorMessages.Add("Not found");
                return null;
            }
            else throw;
        }
    }
}
