namespace DataModel.Repository;

using Microsoft.EntityFrameworkCore;
using DataModel.Model;
using DataModel.Mapper;
using Domain.Model;
using Domain.IRepository;
using Microsoft.EntityFrameworkCore.ChangeTracking;

public class OperatingStaffRepository : GenericRepository<OperatingStaff>, IOperatingStaffRepository
{
    public OperatingStaffRepository(AbsanteeContext context) : base(context!)
    {
    }

    public async Task<OperatingStaff?> GetByMecanographicNumberAsync(string mecanographicNumber)
    {
        try
        {
            OperatingStaffDataModel? staffDataModel = await _context.Set<OperatingStaffDataModel>()
                .FirstOrDefaultAsync(s => s.MecanographicNumber == mecanographicNumber);

            if (staffDataModel == null)
                return null;

            return OperatingStaffMapper.ToDomain(staffDataModel);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<OperatingStaff>> GetByStatusAsync(StaffStatus status)
    {
        try
        {
            List<OperatingStaffDataModel> staffDataModels = await _context.Set<OperatingStaffDataModel>()
                .Where(s => s.Status == (int)status)
                .ToListAsync();

            return OperatingStaffMapper.ToDomainList(staffDataModels);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<OperatingStaff>> SearchAsync(string? name = null, string? email = null, StaffStatus? status = null, string? qualification = null)
    {
        try
        {
            var query = _context.Set<OperatingStaffDataModel>()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(s => s.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(s => s.Email.Contains(email));
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == (int)status.Value);
            }

            // AC4: Filter by qualification
            if (!string.IsNullOrWhiteSpace(qualification))
            {
                // Search for the qualification in the comma-separated string
                // Using Contains to allow partial matching
                query = query.Where(s => s.Qualifications.Contains(qualification));
            }

            List<OperatingStaffDataModel> staffDataModels = await query.ToListAsync();

            return OperatingStaffMapper.ToDomainList(staffDataModels);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> ExistsByMecanographicNumberAsync(string mecanographicNumber)
    {
        return await _context.Set<OperatingStaffDataModel>()
            .AnyAsync(s => s.MecanographicNumber == mecanographicNumber);
    }

    public async Task<List<OperatingStaff>> GetAllAsync()
    {
        try
        {
            List<OperatingStaffDataModel> staffDataModels = await _context.Set<OperatingStaffDataModel>()
                .ToListAsync();

            return OperatingStaffMapper.ToDomainList(staffDataModels);
        }
        catch
        {
            throw;
        }
    }

    public async Task<OperatingStaff> AddAsync(OperatingStaff staff)
    {
        try
        {
            OperatingStaffDataModel? staffDataModel = OperatingStaffMapper.ToDataModel(staff);
            
            if (staffDataModel == null)
                throw new InvalidOperationException("Failed to convert OperatingStaff to DataModel");

            EntityEntry<OperatingStaffDataModel> entityEntry = _context.Set<OperatingStaffDataModel>().Add(staffDataModel);

            await _context.SaveChangesAsync();

            OperatingStaffDataModel savedDataModel = entityEntry.Entity;
            
            var result = OperatingStaffMapper.ToDomain(savedDataModel);
            if (result == null)
                throw new InvalidOperationException("Failed to convert saved DataModel back to Domain");

            return result;
        }
        catch
        {
            throw;
        }
    }

    public async Task<OperatingStaff?> UpdateAsync(OperatingStaff staff)
    {
        try
        {
            OperatingStaffDataModel? staffDataModel = await _context.Set<OperatingStaffDataModel>()
                .FirstOrDefaultAsync(s => s.MecanographicNumber == staff.MecanographicNumber);

            if (staffDataModel == null)
                return null;

            // Update fields (MecanographicNumber is immutable, so we don't update it)
            staffDataModel.Name = staff.Name;
            staffDataModel.Email = staff.Email;
            staffDataModel.PhoneNumber = staff.PhoneNumber;
            staffDataModel.OperationalWindow = staff.OperationalWindow;
            staffDataModel.Qualifications = staff.Qualifications != null && staff.Qualifications.Any()
                ? string.Join(",", staff.Qualifications)
                : string.Empty;
            staffDataModel.Status = (int)staff.Status;
            staffDataModel.StatusHistory = System.Text.Json.JsonSerializer.Serialize(staff.StatusHistory);

            _context.Entry(staffDataModel).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return OperatingStaffMapper.ToDomain(staffDataModel);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ExistsByMecanographicNumberAsync(staff.MecanographicNumber))
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
