using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Model;

namespace Domain.IRepository
{
    /// <summary>
    /// Repository interface for DataRightsRequest entities (US 4.5.3).
    /// </summary>
    public interface IDataRightsRequestRepository
    {
        Task<DataRightsRequest> CreateAsync(DataRightsRequest request);
        Task<DataRightsRequest?> GetByIdAsync(int id);
        Task<IEnumerable<DataRightsRequest>> GetByUserEmailAsync(string userEmail);
        Task<IEnumerable<DataRightsRequest>> GetAllAsync();
        Task<IEnumerable<DataRightsRequest>> GetPendingAsync();
        Task<DataRightsRequest> UpdateAsync(DataRightsRequest request);
        Task<int> CountByUserEmailAsync(string userEmail);
        Task<DateTime?> GetLastRequestDateAsync(string userEmail);
    }
}
