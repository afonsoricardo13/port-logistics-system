namespace DataModel.Repository;

using DataModel.Mapper;
using DataModel.Model;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

public class VesselRepository : GenericRepository<Vessel>, IVesselRepository
{
    private readonly VesselMapper _mapper;

    public VesselRepository(AbsanteeContext context, VesselMapper mapper) : base(context)
    {
        _mapper = mapper;
    }

    public new async Task<Vessel> Add(Vessel vessel)
    {
        var dm = _mapper.ToDataModel(vessel);
        var entry = _context.Set<VesselDataModel>().Add(dm);
        await _context.SaveChangesAsync();
        return _mapper.ToDomain(entry.Entity);
    }

    public async Task<bool> Exists(string imoNumber)
        => await _context.Set<VesselDataModel>().AnyAsync(v => v.ImoNumber == imoNumber);

    public async Task<IEnumerable<Vessel>> GetAllAsync()
    {
        var list = await _context.Set<VesselDataModel>().ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<Vessel?> GetByImoNumberAsync(string imoNumber)
    {
        var dm = await _context.Set<VesselDataModel>().FirstOrDefaultAsync(v => v.ImoNumber == imoNumber);
        return dm == null ? null : _mapper.ToDomain(dm);
    }

    public async Task<IEnumerable<Vessel>> SearchAsync(string? imoNumber, string? name, string? operatorName)
    {
        var query = _context.Set<VesselDataModel>().AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(imoNumber))
            query = query.Where(v => v.ImoNumber.Contains(imoNumber));
        
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(v => v.Name.Contains(name));
        
        if (!string.IsNullOrWhiteSpace(operatorName))
            query = query.Where(v => v.Operator.Contains(operatorName));
        
        var list = await query.ToListAsync();
        return _mapper.ToDomain(list);
    }

    public async Task<Vessel?> Update(Vessel vessel, List<string> errorMessages)
    {
        try
        {
            var dm = await _context.Set<VesselDataModel>().FirstAsync(v => v.ImoNumber == vessel.ImoNumber);
            _mapper.UpdateDataModel(dm, vessel);
            _context.Entry(dm).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return vessel;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await Exists(vessel.ImoNumber))
            {
                errorMessages.Add("Not found");
                return null;
            }
            else throw;
        }
    }
}
