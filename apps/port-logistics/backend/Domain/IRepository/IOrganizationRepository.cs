using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.IRepository
{
    public interface IOrganizationRepository
    {
        Task<Model.Organization?> GetByIdAsync(long id);
        Task<Model.Organization?> GetByIdentifierAsync(string identifier);
        Task<IEnumerable<Model.Organization>> GetAllAsync();
        Task<Model.Organization> AddAsync(Model.Organization organization);
        Task<Model.Organization> UpdateAsync(Model.Organization organization);
        Task<bool> ExistsByIdentifierAsync(string identifier);
        Task<bool> ExistsByTaxNumberAsync(string taxNumber);
    }
}
