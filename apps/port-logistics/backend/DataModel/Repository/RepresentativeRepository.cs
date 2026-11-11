using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataModel.Mapper;
using DataModel.Model;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel.Repository
{
    public class RepresentativeRepository : IRepresentativeRepository
    {
        private readonly AbsanteeContext _context;
        private readonly RepresentativeMapper _mapper;

        public RepresentativeRepository(AbsanteeContext context, RepresentativeMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Representative?> GetByIdAsync(long id)
        {
            var dataModel = await _context.Representatives
                .FirstOrDefaultAsync(r => r.Id == id);

            return dataModel == null ? null : _mapper.ToDomain(dataModel);
        }

        public async Task<Representative?> GetByEmailAsync(string email)
        {
            var dataModel = await _context.Representatives
                .FirstOrDefaultAsync(r => r.Email == email);

            return dataModel == null ? null : _mapper.ToDomain(dataModel);
        }

        public async Task<IEnumerable<Representative>> GetByOrganizationIdAsync(long organizationId)
        {
            var dataModels = await _context.Representatives
                .Where(r => r.OrganizationId == organizationId)
                .ToListAsync();

            return dataModels.Select(_mapper.ToDomain);
        }

        public async Task<IEnumerable<Representative>> GetAllAsync()
        {
            var dataModels = await _context.Representatives.ToListAsync();
            return dataModels.Select(_mapper.ToDomain);
        }

        public async Task<Representative> AddAsync(Representative representative)
        {
            var dataModel = _mapper.ToDataModel(representative);
            _context.Representatives.Add(dataModel);
            await _context.SaveChangesAsync();

            return _mapper.ToDomain(dataModel);
        }

        public async Task<Representative> UpdateAsync(Representative representative)
        {
            var dataModel = await _context.Representatives
                .FirstOrDefaultAsync(r => r.Id == representative.Id);

            if (dataModel == null)
                throw new KeyNotFoundException($"Representative with ID {representative.Id} not found.");

            _mapper.UpdateDataModel(representative, dataModel);
            await _context.SaveChangesAsync();

            return _mapper.ToDomain(dataModel);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Representatives.AnyAsync(r => r.Email == email);
        }

        public async Task<bool> ExistsByCitizenIdAsync(string citizenId)
        {
            return await _context.Representatives.AnyAsync(r => r.CitizenId == citizenId);
        }
    }
}
