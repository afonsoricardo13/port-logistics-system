namespace DataModel.Repository;

using DataModel.Mapper;
using DataModel.Model;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

public class PhysicalResourceRepository : GenericRepository<PhysicalResource>, IPhysicalResourceRepository
{
    private readonly PhysicalResourceMapper _mapper;

    public PhysicalResourceRepository(AbsanteeContext context, PhysicalResourceMapper mapper) : base(context)
    {
        _mapper = mapper;
    }

    public new async Task<PhysicalResource> Add(PhysicalResource resource)
    {
        var dm = _mapper.ToDataModel(resource);
        var entry = _context.Set<PhysicalResourceDataModel>().Add(dm);
        await _context.SaveChangesAsync();
        return _mapper.ToDomain(entry.Entity);
    }

    public async Task<bool> Exists(string code)
        => await _context.Set<PhysicalResourceDataModel>().AnyAsync(v => v.Code == code);

    public new IEnumerable<PhysicalResource> GetAll()
    {
        var list = _context.Set<PhysicalResourceDataModel>().ToList();
        return _mapper.ToDomainList(list);
    }

    public async Task<IEnumerable<PhysicalResource>> GetAllAsync()
    {
        var list = await _context.Set<PhysicalResourceDataModel>().ToListAsync();
        return _mapper.ToDomainList(list);
    }

    public async Task<PhysicalResource?> GetByCodeAsync(string code)
    {
        var dm = await _context.Set<PhysicalResourceDataModel>().FirstOrDefaultAsync(v => v.Code == code);
        return dm == null ? null : _mapper.ToDomain(dm);
    }

    public async Task<IEnumerable<PhysicalResource>> SearchAsync(string? code, string? description, string? type, string? status)
    {
        var query = _context.Set<PhysicalResourceDataModel>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(code))
            query = query.Where(v => v.Code.Contains(code));

        if (!string.IsNullOrWhiteSpace(description))
            query = query.Where(v => v.Description.Contains(description));

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(v => v.Type.Contains(type));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status.Contains(status));

        var list = await query.ToListAsync();
        return _mapper.ToDomainList(list);
    }

    public async Task<PhysicalResource?> Update(PhysicalResource resource, List<string> errorMessages)
    {
        try
        {
            var dm = await _context.Set<PhysicalResourceDataModel>().FirstAsync(v => v.Code == resource.Code);
            _mapper.UpdateDataModel(dm, resource);
            _context.Entry(dm).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return resource;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await Exists(resource.Code))
            {
                errorMessages.Add("Not found");
                return null;
            }
            else throw;
        }
    }
}
