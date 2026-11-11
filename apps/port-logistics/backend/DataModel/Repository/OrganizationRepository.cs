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
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly AbsanteeContext _context;
        private readonly OrganizationMapper _mapper;

        public OrganizationRepository(AbsanteeContext context, OrganizationMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Organization?> GetByIdAsync(long id)
        {
            var dataModel = await _context.Organizations
                .Include(o => o.Representatives)
                .FirstOrDefaultAsync(o => o.Id == id);

            return dataModel == null ? null : _mapper.ToDomain(dataModel);
        }

        public async Task<Organization?> GetByIdentifierAsync(string identifier)
        {
            var dataModel = await _context.Organizations
                .Include(o => o.Representatives)
                .FirstOrDefaultAsync(o => o.Identifier == identifier);

            return dataModel == null ? null : _mapper.ToDomain(dataModel);
        }

        public async Task<IEnumerable<Organization>> GetAllAsync()
        {
            var dataModels = await _context.Organizations
                .Include(o => o.Representatives)
                .ToListAsync();

            return dataModels.Select(_mapper.ToDomain);
        }

        public async Task<Organization> AddAsync(Organization organization)
        {
            var dataModel = _mapper.ToDataModel(organization);
            _context.Organizations.Add(dataModel);
            await _context.SaveChangesAsync();

            return _mapper.ToDomain(dataModel);
        }

        public async Task<Organization> UpdateAsync(Organization organization)
        {
            var dataModel = await _context.Organizations
                .Include(o => o.Representatives)
                .FirstOrDefaultAsync(o => o.Id == organization.Id);

            if (dataModel == null)
                throw new KeyNotFoundException($"Organization with ID {organization.Id} not found.");

            _mapper.UpdateDataModel(organization, dataModel);
            await _context.SaveChangesAsync();

            return _mapper.ToDomain(dataModel);
        }

        public async Task<bool> ExistsByIdentifierAsync(string identifier)
        {
            return await _context.Organizations.AnyAsync(o => o.Identifier == identifier);
        }

        public async Task<bool> ExistsByTaxNumberAsync(string taxNumber)
        {
            return await _context.Organizations.AnyAsync(o => o.TaxNumber == taxNumber);
        }
    }
}
