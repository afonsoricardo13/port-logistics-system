using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTO;
using Domain.Factory;
using Domain.IRepository;

namespace Application.Services
{
    public class RepresentativeService
    {
        private readonly IRepresentativeRepository _representativeRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IRepresentativeFactory _representativeFactory;
        private readonly IUserManagementService _userManagementService;

        public RepresentativeService(
            IRepresentativeRepository representativeRepository,
            IOrganizationRepository organizationRepository,
            IRepresentativeFactory representativeFactory,
            IUserManagementService userManagementService)
        {
            _representativeRepository = representativeRepository 
                ?? throw new ArgumentNullException(nameof(representativeRepository));
            _organizationRepository = organizationRepository 
                ?? throw new ArgumentNullException(nameof(organizationRepository));
            _representativeFactory = representativeFactory 
                ?? throw new ArgumentNullException(nameof(representativeFactory));
            _userManagementService = userManagementService
                ?? throw new ArgumentNullException(nameof(userManagementService));
        }

        public async Task<RepresentativeDTO?> GetByIdAsync(long id)
        {
            var representative = await _representativeRepository.GetByIdAsync(id);
            return representative == null ? null : RepresentativeDTO.FromDomain(representative);
        }

        public async Task<IEnumerable<RepresentativeDTO>> GetByOrganizationIdAsync(long organizationId)
        {
            var representatives = await _representativeRepository.GetByOrganizationIdAsync(organizationId);
            return representatives.Select(RepresentativeDTO.FromDomain);
        }

        public async Task<IEnumerable<RepresentativeDTO>> GetAllAsync()
        {
            var representatives = await _representativeRepository.GetAllAsync();
            return representatives.Select(RepresentativeDTO.FromDomain);
        }

        public async Task<RepresentativeWithOrganizationDTO?> GetByEmailAsync(string email)
        {
            var rep = await _representativeRepository.GetByEmailAsync(email);
            if (rep == null) return null;

            var org = await _organizationRepository.GetByIdAsync(rep.OrganizationId);
            return new RepresentativeWithOrganizationDTO
            {
                OrganizationIdentifier = org?.Identifier ?? string.Empty
            };
        }

        public async Task<RepresentativeDTO> CreateAsync(long organizationId, CreateRepresentativeDTO dto)
        {
            // Validate organization exists
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                throw new InvalidOperationException(
                    $"Organization with ID {organizationId} does not exist.");
            }

            // Validate uniqueness
            if (await _representativeRepository.ExistsByEmailAsync(dto.Email))
            {
                throw new InvalidOperationException(
                    $"A representative with email '{dto.Email}' already exists.");
            }

            if (await _representativeRepository.ExistsByCitizenIdAsync(dto.CitizenId))
            {
                throw new InvalidOperationException(
                    $"A representative with citizen ID '{dto.CitizenId}' already exists.");
            }

            // Create representative
            var representative = _representativeFactory.CreateRepresentative(
                dto.CitizenId,
                dto.Name,
                dto.Nationality,
                dto.Email,
                dto.PhoneNumber,
                organizationId
            );

            // Persist representative
            var createdRepresentative = await _representativeRepository.AddAsync(representative);

            // Automatically create SystemUser with ShippingAgentRepresentative role
            // This ensures the representative can authenticate and access the system
            try
            {
                var existingUser = await _userManagementService.GetUserByEmailAsync(dto.Email);
                if (existingUser == null)
                {
                    // Create new SystemUser for this representative
                    var systemUserDto = new CreateSystemUserDTO
                    {
                        Email = dto.Email,
                        Name = dto.Name,
                        Role = "ShippingAgentRepresentative"
                    };

                    await _userManagementService.CreateUserAsync(systemUserDto, "System");
                }
                else if (existingUser.Role != "ShippingAgentRepresentative")
                {
                    // Update existing user's role to ShippingAgentRepresentative
                    var updateUserDto = new UpdateSystemUserDTO
                    {
                        Email = dto.Email,
                        Name = dto.Name,
                        Role = "ShippingAgentRepresentative",
                        Active = existingUser.Active
                    };

                    await _userManagementService.UpdateUserAsync(updateUserDto);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail representative creation
                // SystemUser can be created manually later if needed
                Console.WriteLine($"Warning: Failed to create/update SystemUser for representative {dto.Email}: {ex.Message}");
            }

            return RepresentativeDTO.FromDomain(createdRepresentative);
        }

        public async Task<RepresentativeDTO?> UpdateAsync(long id, UpdateRepresentativeDTO dto)
        {
            var representative = await _representativeRepository.GetByIdAsync(id);
            if (representative == null)
                return null;

            representative.UpdateDetails(
                dto.Name,
                dto.Nationality,
                dto.Email,
                dto.PhoneNumber
            );

            var updatedRepresentative = await _representativeRepository.UpdateAsync(representative);
            return RepresentativeDTO.FromDomain(updatedRepresentative);
        }

        public async Task<bool> DeactivateAsync(long id)
        {
            var representative = await _representativeRepository.GetByIdAsync(id);
            if (representative == null)
                return false;

            // Check if this is the only active representative for the organization
            var orgRepresentatives = await _representativeRepository.GetByOrganizationIdAsync(
                representative.OrganizationId);
            
            var activeCount = orgRepresentatives.Count(r => r.IsActive);
            if (activeCount <= 1)
            {
                throw new InvalidOperationException(
                    "Cannot deactivate the last active representative of an organization.");
            }

            representative.Deactivate();
            await _representativeRepository.UpdateAsync(representative);

            // Deactivate the SystemUser as well
            try
            {
                var existingUser = await _userManagementService.GetUserByEmailAsync(representative.Email);
                if (existingUser != null && existingUser.Active)
                {
                    var updateUserDto = new UpdateSystemUserDTO
                    {
                        Email = representative.Email,
                        Active = false
                    };
                    await _userManagementService.UpdateUserAsync(updateUserDto);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to deactivate SystemUser for representative {representative.Email}: {ex.Message}");
            }

            return true;
        }

        public async Task<bool> ReactivateAsync(long id)
        {
            var representative = await _representativeRepository.GetByIdAsync(id);
            if (representative == null)
                return false;

            representative.Reactivate();
            await _representativeRepository.UpdateAsync(representative);

            // Reactivate the SystemUser as well
            try
            {
                var existingUser = await _userManagementService.GetUserByEmailAsync(representative.Email);
                if (existingUser != null && !existingUser.Active)
                {
                    var updateUserDto = new UpdateSystemUserDTO
                    {
                        Email = representative.Email,
                        Active = true
                    };
                    await _userManagementService.UpdateUserAsync(updateUserDto);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to reactivate SystemUser for representative {representative.Email}: {ex.Message}");
            }

            return true;
        }
    }
}
