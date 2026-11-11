using DataModel.Model;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository
{
    /// <summary>
    /// Repository implementation for Privacy Policy management (US 4.5.1).
    /// Provides data access for GDPR-compliant privacy policy versioning and history.
    /// </summary>
    public class PrivacyPolicyRepository : IPrivacyPolicyRepository
    {
        private readonly AbsanteeContext _context;

        public PrivacyPolicyRepository(AbsanteeContext context)
        {
            _context = context;
        }

        public async Task<PrivacyPolicy?> GetCurrentAsync()
        {
            return await _context.Set<PrivacyPolicy>()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Version)
                .FirstOrDefaultAsync();
        }

        public async Task<PrivacyPolicy?> GetByIdAsync(int id)
        {
            return await _context.Set<PrivacyPolicy>()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PrivacyPolicy?> GetByVersionAsync(int version)
        {
            return await _context.Set<PrivacyPolicy>()
                .FirstOrDefaultAsync(p => p.Version == version);
        }

        public async Task<IEnumerable<PrivacyPolicy>> GetHistoryAsync()
        {
            return await _context.Set<PrivacyPolicy>()
                .OrderByDescending(p => p.Version)
                .ToListAsync();
        }

        public async Task<PrivacyPolicy> CreateAsync(PrivacyPolicy policy)
        {
            var entry = await _context.Set<PrivacyPolicy>().AddAsync(policy);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<PrivacyPolicy> UpdateAsync(PrivacyPolicy policy)
        {
            _context.Set<PrivacyPolicy>().Update(policy);
            await _context.SaveChangesAsync();
            return policy;
        }

        public async Task DeactivateAllAsync()
        {
            var activePolicies = await _context.Set<PrivacyPolicy>()
                .Where(p => p.IsActive)
                .ToListAsync();

            foreach (var policy in activePolicies)
            {
                policy.IsActive = false;
            }

            if (activePolicies.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetNextVersionNumberAsync()
        {
            var maxVersion = await _context.Set<PrivacyPolicy>()
                .MaxAsync(p => (int?)p.Version) ?? 0;

            return maxVersion + 1;
        }

        public async Task<bool> VersionExistsAsync(int version)
        {
            return await _context.Set<PrivacyPolicy>()
                .AnyAsync(p => p.Version == version);
        }
    }
}
