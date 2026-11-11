using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.IRepository
{
    public interface IRepresentativeRepository
    {
        Task<Model.Representative?> GetByIdAsync(long id);
        Task<Model.Representative?> GetByEmailAsync(string email);
        Task<IEnumerable<Model.Representative>> GetByOrganizationIdAsync(long organizationId);
        Task<IEnumerable<Model.Representative>> GetAllAsync();
        Task<Model.Representative> AddAsync(Model.Representative representative);
        Task<Model.Representative> UpdateAsync(Model.Representative representative);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByCitizenIdAsync(string citizenId);
    }
}
