using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTO;
using Domain.Factory;
using Domain.IRepository;
using Domain.Model;

namespace Application.Services
{
    public class OrganizationService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IRepresentativeRepository _representativeRepository;
        private readonly IOrganizationFactory _organizationFactory;
        private readonly IRepresentativeFactory _representativeFactory;
        private readonly IUserManagementService _userManagementService;

        public OrganizationService(
            IOrganizationRepository organizationRepository,
            IRepresentativeRepository representativeRepository,
            IOrganizationFactory organizationFactory,
            IRepresentativeFactory representativeFactory,
            IUserManagementService userManagementService)
        {
            _organizationRepository = organizationRepository 
                ?? throw new ArgumentNullException(nameof(organizationRepository));
            _representativeRepository = representativeRepository 
                ?? throw new ArgumentNullException(nameof(representativeRepository));
            _organizationFactory = organizationFactory 
                ?? throw new ArgumentNullException(nameof(organizationFactory));
            _representativeFactory = representativeFactory 
                ?? throw new ArgumentNullException(nameof(representativeFactory));
            _userManagementService = userManagementService 
                ?? throw new ArgumentNullException(nameof(userManagementService));
        }

        public async Task<OrganizationDTO?> GetByIdAsync(long id)
        {
            var organization = await _organizationRepository.GetByIdAsync(id);
            return organization == null ? null : OrganizationDTO.FromDomain(organization);
        }

        public async Task<OrganizationDTO?> GetByIdentifierAsync(string identifier)
        {
            var organization = await _organizationRepository.GetByIdentifierAsync(identifier);
            return organization == null ? null : OrganizationDTO.FromDomain(organization);
        }

        public async Task<IEnumerable<OrganizationDTO>> GetAllAsync()
        {
            var organizations = await _organizationRepository.GetAllAsync();
            return organizations.Select(OrganizationDTO.FromDomain);
        }

        public async Task<OrganizationDTO> CreateAsync(CreateOrganizationDTO dto)
        {
            // Validate uniqueness
            if (await _organizationRepository.ExistsByIdentifierAsync(dto.Identifier))
            {
                throw new InvalidOperationException(
                    $"An organization with identifier '{dto.Identifier}' already exists.");
            }

            if (await _organizationRepository.ExistsByTaxNumberAsync(dto.TaxNumber))
            {
                throw new InvalidOperationException(
                    $"An organization with tax number '{dto.TaxNumber}' already exists.");
            }

            if (await _representativeRepository.ExistsByEmailAsync(dto.InitialRepresentative.Email))
            {
                throw new InvalidOperationException(
                    $"A representative with email '{dto.InitialRepresentative.Email}' already exists.");
            }

            if (await _representativeRepository.ExistsByCitizenIdAsync(dto.InitialRepresentative.CitizenId))
            {
                throw new InvalidOperationException(
                    $"A representative with citizen ID '{dto.InitialRepresentative.CitizenId}' already exists.");
            }

            // Create address
            var address = dto.Address.ToDomain();

            // Create initial representative with temporary organizationId = 0
            // EF Core will set the correct OrganizationId during persistence
            var initialRepresentative = _representativeFactory.CreateRepresentative(
                dto.InitialRepresentative.CitizenId,
                dto.InitialRepresentative.Name,
                dto.InitialRepresentative.Nationality,
                dto.InitialRepresentative.Email,
                dto.InitialRepresentative.PhoneNumber,
                0
            );

            // Create organization with initial representative
            var organization = _organizationFactory.CreateOrganization(
                dto.Identifier,
                dto.LegalName,
                dto.AlternativeName,
                dto.TaxNumber,
                address,
                initialRepresentative
            );

            // Persist organization (cascade saves representative with correct OrganizationId)
            var createdOrganization = await _organizationRepository.AddAsync(organization);

            // US 2.2.5: Create SystemUser for initial representative and send activation email
            try
            {
                var existingUser = await _userManagementService.GetUserByEmailAsync(dto.InitialRepresentative.Email);
                if (existingUser == null)
                {
                    // Create new SystemUser for initial representative
                    var systemUserDto = new CreateSystemUserDTO
                    {
                        Email = dto.InitialRepresentative.Email,
                        Name = dto.InitialRepresentative.Name,
                        Role = "ShippingAgentRepresentative"
                    };
                    await _userManagementService.CreateUserAsync(systemUserDto, "System");
                }
                else
                {
                    // Update existing user to include ShippingAgentRepresentative role
                    var updateUserDto = new UpdateSystemUserDTO
                    {
                        Email = dto.InitialRepresentative.Email,
                        Name = dto.InitialRepresentative.Name,
                        Role = "ShippingAgentRepresentative"
                    };
                    await _userManagementService.UpdateUserAsync(updateUserDto);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - SystemUser can be created manually later if needed
                Console.WriteLine($"Warning: Failed to create/update SystemUser for initial representative {dto.InitialRepresentative.Email}: {ex.Message}");
            }

            return OrganizationDTO.FromDomain(createdOrganization);
        }

        public async Task<OrganizationDTO?> UpdateAsync(long id, UpdateOrganizationDTO dto)
        {
            var organization = await _organizationRepository.GetByIdAsync(id);
            if (organization == null)
                return null;

            var address = dto.Address.ToDomain();
            organization.UpdateDetails(
                dto.LegalName,
                dto.AlternativeName,
                dto.TaxNumber,
                address
            );

            var updatedOrganization = await _organizationRepository.UpdateAsync(organization);
            return OrganizationDTO.FromDomain(updatedOrganization);
        }
    }
}
