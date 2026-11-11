namespace Domain.IRepository;

using Domain.Model;

public interface IOperatingStaffRepository : IGenericRepository<OperatingStaff>
{
    Task<List<OperatingStaff>> GetAllAsync();
    Task<OperatingStaff> AddAsync(OperatingStaff staff);
    Task<OperatingStaff?> UpdateAsync(OperatingStaff staff);
    Task<OperatingStaff?> GetByMecanographicNumberAsync(string mecanographicNumber);
    Task<List<OperatingStaff>> GetByStatusAsync(StaffStatus status);
    Task<List<OperatingStaff>> SearchAsync(string? name = null, string? email = null, StaffStatus? status = null, string? qualification = null);
    Task<bool> ExistsByMecanographicNumberAsync(string mecanographicNumber);
}
