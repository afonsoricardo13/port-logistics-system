namespace DataModel.Repository;

using Microsoft.EntityFrameworkCore;
using DataModel.Model;
using DataModel.Mapper;
using Domain.Model;
using Domain.IRepository;
using Microsoft.EntityFrameworkCore.ChangeTracking;

// US 2.2.13
public class QualificationRepository : GenericRepository<Qualification>, IQualificationRepository
{
    public QualificationRepository(AbsanteeContext context) : base(context!)
    {
    }

    public async Task<List<Qualification>> GetAllAsync()
    {
        try
        {
            List<QualificationDataModel> qualificationDataModels = await _context.Set<QualificationDataModel>()
                .ToListAsync();

            return QualificationMapper.ToDomainList(qualificationDataModels);
        }
        catch
        {
            throw;
        }
    }

    public async Task<Qualification?> GetByQualificationNameAsync(string qualificationName)
    {
        try
        {
            string normalizedName = qualificationName.Trim();

            QualificationDataModel? qualificationDataModel = await _context.Set<QualificationDataModel>()
                .FirstOrDefaultAsync(q => q.QualificationName.ToLower() == normalizedName.ToLower());

            if (qualificationDataModel == null)
                return null;

            return QualificationMapper.ToDomain(qualificationDataModel);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<Qualification>> SearchAsync(string? namePattern = null)
    {
        try
        {
            var query = _context.Set<QualificationDataModel>()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(namePattern))
            {
                string normalizedPattern = namePattern.Trim().ToLower();
                query = query.Where(q => q.QualificationName.ToLower().Contains(normalizedPattern));
            }

            List<QualificationDataModel> qualificationDataModels = await query.ToListAsync();

            return QualificationMapper.ToDomainList(qualificationDataModels);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> ExistsByQualificationNameAsync(string qualificationName)
    {
        string normalizedName = qualificationName.Trim().ToLower();
        return await _context.Set<QualificationDataModel>()
            .AnyAsync(q => q.QualificationName.ToLower() == normalizedName);
    }

    /// <summary>
    /// Adds a new qualification
    /// </summary>
    public async Task<Qualification> AddAsync(Qualification qualification)
    {
        try
        {
            QualificationDataModel? qualificationDataModel = QualificationMapper.ToDataModel(qualification);
            
            if (qualificationDataModel == null)
                throw new InvalidOperationException("Failed to convert Qualification to DataModel");

            EntityEntry<QualificationDataModel> entityEntry = _context.Set<QualificationDataModel>()
                .Add(qualificationDataModel);

            await _context.SaveChangesAsync();

            QualificationDataModel savedDataModel = entityEntry.Entity;
            
            var result = QualificationMapper.ToDomain(savedDataModel);
            if (result == null)
                throw new InvalidOperationException("Failed to convert saved DataModel back to Domain");

            return result;
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Updates an existing qualification
    /// US 2.2.13: Manage qualifications - only requiredNumber can be updated (name is the ID and is immutable)
    /// </summary>
    public async Task<Qualification?> UpdateAsync(Qualification qualification)
    {
        try
        {
            QualificationDataModel? qualificationDataModel = await _context.Set<QualificationDataModel>()
                .FirstOrDefaultAsync(q => q.QualificationName == qualification.QualificationName);

            if (qualificationDataModel == null)
                return null;

            // Update fields (QualificationName is immutable as it's the primary key)
            qualificationDataModel.RequiredNumber = qualification.RequiredNumber;

            _context.Entry(qualificationDataModel).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return QualificationMapper.ToDomain(qualificationDataModel);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ExistsByQualificationNameAsync(qualification.QualificationName))
            {
                return null;
            }
            else
            {
                throw;
            }
        }
        catch
        {
            throw;
        }
    }
}
