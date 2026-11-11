using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository
{
    /// <summary>
    /// Repository implementation for DataRightsRequest entities (US 4.5.3).
    /// </summary>
    public class DataRightsRequestRepository : IDataRightsRequestRepository
    {
        private readonly AbsanteeContext _context;

        public DataRightsRequestRepository(AbsanteeContext context)
        {
            _context = context;
        }

        public async Task<DataRightsRequest> CreateAsync(DataRightsRequest request)
        {
            _context.DataRightsRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<DataRightsRequest?> GetByIdAsync(int id)
        {
            return await _context.DataRightsRequests.FindAsync(id);
        }

        public async Task<IEnumerable<DataRightsRequest>> GetByUserEmailAsync(string userEmail)
        {
            return await _context.DataRightsRequests
                .Where(r => r.UserEmail == userEmail)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DataRightsRequest>> GetAllAsync()
        {
            return await _context.DataRightsRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DataRightsRequest>> GetPendingAsync()
        {
            return await _context.DataRightsRequests
                .Where(r => r.Status == 0 || r.Status == 1) // Pending or InProgress
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<DataRightsRequest> UpdateAsync(DataRightsRequest request)
        {
            _context.DataRightsRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<int> CountByUserEmailAsync(string userEmail)
        {
            return await _context.DataRightsRequests
                .CountAsync(r => r.UserEmail == userEmail);
        }

        public async Task<DateTime?> GetLastRequestDateAsync(string userEmail)
        {
            var lastRequest = await _context.DataRightsRequests
                .Where(r => r.UserEmail == userEmail)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
            
            return lastRequest?.CreatedAt;
        }
    }
}
