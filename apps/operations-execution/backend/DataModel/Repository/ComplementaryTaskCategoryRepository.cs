using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository;

public class ComplementaryTaskCategoryRepository : IComplementaryTaskCategoryRepository
{
    private readonly OEMDbContext _context;

    public ComplementaryTaskCategoryRepository(OEMDbContext context)
    {
        _context = context;
    }

    public async Task<ComplementaryTaskCategory?> GetByCodeAsync(string code)
    {
        return await _context.ComplementaryTaskCategories.FindAsync(code);
    }

    public async Task<IEnumerable<ComplementaryTaskCategory>> GetAllAsync()
    {
        return await _context.ComplementaryTaskCategories
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<ComplementaryTaskCategory> AddAsync(ComplementaryTaskCategory category)
    {
        await _context.ComplementaryTaskCategories.AddAsync(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<ComplementaryTaskCategory> UpdateAsync(ComplementaryTaskCategory category)
    {
        _context.ComplementaryTaskCategories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteAsync(string code)
    {
        var category = await GetByCodeAsync(code);
        if (category == null)
            return false;

        _context.ComplementaryTaskCategories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string code)
    {
        return await _context.ComplementaryTaskCategories.AnyAsync(c => c.Code == code);
    }
}
