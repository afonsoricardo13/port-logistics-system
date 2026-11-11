/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTO;
using Application.Services;
using Domain.Factory;
using Domain.IRepository;
using Moq;
using Xunit;

namespace Application.Tests
{
    public class RepresentativeServiceTests
    {
        private readonly Mock<IRepresentativeRepository> _mockRepresentativeRepository;
        private readonly Mock<IOrganizationRepository> _mockOrganizationRepository;
        private readonly Mock<IRepresentativeFactory> _mockRepresentativeFactory;
        private readonly Mock<IUserManagementService> _mockUserManagementService;
        private readonly RepresentativeService _service;

        public RepresentativeServiceTests()
        {
            _mockRepresentativeRepository = new Mock<IRepresentativeRepository>();
            _mockOrganizationRepository = new Mock<IOrganizationRepository>();
            _mockRepresentativeFactory = new Mock<IRepresentativeFactory>();
            _mockUserManagementService = new Mock<IUserManagementService>();

            _service = new RepresentativeService(
                _mockRepresentativeRepository.Object,
                _mockOrganizationRepository.Object,
                _mockRepresentativeFactory.Object,
                _mockUserManagementService.Object
            );
        }

        #region Get / List Tests

        [Fact]
        public async Task GetByIdAsync_WhenRepresentativeExists_ShouldReturnDto()
        {
            // Arrange
            var domainRep = Mock.Of<Domain.Model.Representative>();
            _mockRepresentativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(domainRep);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            _mockRepresentativeRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetByOrganizationIdAsync_ShouldReturnList()
        {
            // Arrange
            var list = new List<Domain.Model.Representative>
            {
                Mock.Of<Domain.Model.Representative>(),
                Mock.Of<Domain.Model.Representative>()
            };
            _mockRepresentativeRepository.Setup(r => r.GetByOrganizationIdAsync(5)).ReturnsAsync(list);

            // Act
            var result = await _service.GetByOrganizationIdAsync(5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockRepresentativeRepository.Verify(r => r.GetByOrganizationIdAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnList()
        {
            // Arrange
            var list = new List<Domain.Model.Representative>
            {
                Mock.Of<Domain.Model.Representative>(),
                Mock.Of<Domain.Model.Representative>()
            };
            _mockRepresentativeRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockRepresentativeRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByEmailAsync_WhenExists_ShouldReturnRepresentativeWithOrganizationIdentifier()
        {
            // Arrange
            var domainRep = Mock.Of<Domain.Model.Representative>(r => r.OrganizationId == 42);
            _mockRepresentativeRepository.Setup(r => r.GetByEmailAsync("rep@org.com")).ReturnsAsync(domainRep);

            var domainOrg = Mock.Of<Domain.Model.Organization>(o => o.Identifier == "ORG-42");
            _mockOrganizationRepository.Setup(o => o.GetByIdAsync(42)).ReturnsAsync(domainOrg);

            // Act
            var result = await _service.GetByEmailAsync("rep@org.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ORG-42", result.OrganizationIdentifier);
            _mockRepresentativeRepository.Verify(r => r.GetByEmailAsync("rep@org.com"), Times.Once);
            _mockOrganizationRepository.Verify(o => o.GetByIdAsync(42), Times.Once);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WhenOrganizationNotFound_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dto = new CreateRepresentativeDTO { Email = "a@b.com", CitizenId = "C1", Name = "Name", Nationality = "PT", PhoneNumber = "+351" };
            _mockOrganizationRepository.Setup(o => o.GetByIdAsync(99)).ReturnsAsync((Domain.Model.Organization?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(99, dto));
            _mockRepresentativeRepository.Verify(r => r.AddAsync(It.IsAny<Domain.Model.Representative>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenEmailAlreadyExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var dto = new CreateRepresentativeDTO { Email = "exists@org.com", CitizenId = "C1", Name = "Name", Nationality = "PT", PhoneNumber = "+351" };
            _mockOrganizationRepository.Setup(o => o.GetByIdAsync(1)).ReturnsAsync(Mock.Of<Domain.Model.Organization>());
            _mockRepresentativeRepository.Setup(r => r.ExistsByEmailAsync("exists@org.com")).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(1, dto));
            _mockRepresentativeRepository.Verify(r => r.AddAsync(It.IsAny<Domain.Model.Representative>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithUniqueData_ShouldCreateRepresentative_AndCreateSystemUserWhenNotExists()
        {
            // Arrange
            var dto = new CreateRepresentativeDTO
            {
                Email = "new@org.com",
                CitizenId = "C-1",
                Name = "Rep Name",
                Nationality = "PT",
                PhoneNumber = "+351900000000"
            };

            _mockOrganizationRepository.Setup(o => o.GetByIdAsync(10)).ReturnsAsync(Mock.Of<Domain.Model.Organization>());
            _mockRepresentativeRepository.Setup(r => r.ExistsByEmailAsync("new@org.com")).ReturnsAsync(false);
            _mockRepresentativeRepository.Setup(r => r.ExistsByCitizenIdAsync("C-1")).ReturnsAsync(false);

            var createdDomainRep = Mock.Of<Domain.Model.Representative>();
            _mockRepresentativeFactory.Setup(f => f.CreateRepresentative(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .Returns(createdDomainRep);

            _mockRepresentativeRepository.Setup(r => r.AddAsync(createdDomainRep)).ReturnsAsync(createdDomainRep);

            // return null => user does not exist
            _mockUserManagementService.Setup(u => u.GetUserByEmailAsync("new@org.com")).ReturnsAsync((SystemUserDTO?)null);

            // Act
            var result = await _service.CreateAsync(10, dto);

            // Assert
            Assert.NotNull(result);
            _mockRepresentativeRepository.Verify(r => r.AddAsync(createdDomainRep), Times.Once);
            _mockUserManagementService.Verify(u => u.GetUserByEmailAsync("new@org.com"), Times.Once);
            _mockUserManagementService.Verify(u => u.CreateUserAsync(It.IsAny<CreateSystemUserDTO>(), "System"), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenUserAlreadyExistsWithDifferentRole_ShouldUpdateUserRole()
        {
            // Arrange
            var dto = new CreateRepresentativeDTO
            {
                Email = "existsrole@org.com",
                CitizenId = "C-2",
                Name = "Rep Name",
                Nationality = "PT",
                PhoneNumber = "+351900000001"
            };

            _mockOrganizationRepository.Setup(o => o.GetByIdAsync(20)).ReturnsAsync(Mock.Of<Domain.Model.Organization>());
            _mockRepresentativeRepository.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
            _mockRepresentativeRepository.Setup(r => r.ExistsByCitizenIdAsync(dto.CitizenId)).ReturnsAsync(false);

            var createdDomainRep = Mock.Of<Domain.Model.Representative>();
            _mockRepresentativeFactory.Setup(f => f.CreateRepresentative(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .Returns(createdDomainRep);

            _mockRepresentativeRepository.Setup(r => r.AddAsync(createdDomainRep)).ReturnsAsync(createdDomainRep);

            // existing user with different role -> return SystemUserDTO
            var existingUser = new SystemUserDTO { Role = "SomeOtherRole", Active = true, Email = dto.Email, Name = dto.Name };
            _mockUserManagementService.Setup(u => u.GetUserByEmailAsync(dto.Email)).ReturnsAsync(existingUser);

            // Act
            var result = await _service.CreateAsync(20, dto);

            // Assert
            Assert.NotNull(result);
            _mockUserManagementService.Verify(u => u.GetUserByEmailAsync(dto.Email), Times.Once);
            _mockUserManagementService.Verify(u => u.UpdateUserAsync(It.IsAny<UpdateSystemUserDTO>()), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WhenRepresentativeDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            _mockRepresentativeRepository.Setup(r => r.GetByIdAsync(123)).ReturnsAsync((Domain.Model.Representative?)null);
            var dto = new UpdateRepresentativeDTO { Name = "New", Nationality = "PT", Email = "e@e.com", PhoneNumber = "+351" };

            // Act
            var result = await _service.UpdateAsync(123, dto);

            // Assert
            Assert.Null(result);
            _mockRepresentativeRepository.Verify(r => r.GetByIdAsync(123), Times.Once);
            _mockRepresentativeRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.Model.Representative>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenRepresentativeExists_ShouldUpdateAndReturnDto()
        {
            // Arrange
            var domainRep = Mock.Of<Domain.Model.Representative>();
            _mockRepresentativeRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(domainRep);
            _mockRepresentativeRepository.Setup(r => r.UpdateAsync(domainRep)).ReturnsAsync(domainRep);
            var dto = new UpdateRepresentativeDTO { Name = "Updated", Nationality = "PT", Email = "u@u.com", PhoneNumber = "+351" };

            // Act
            var result = await _service.UpdateAsync(10, dto);

            // Assert
            Assert.NotNull(result);
            _mockRepresentativeRepository.Verify(r => r.UpdateAsync(domainRep), Times.Once);
        }

        #endregion

        #region Deactivate / Reactivate Tests

        [Fact]
        public async Task DeactivateAsync_WhenRepresentativeNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockRepresentativeRepository.Setup(r => r.GetByIdAsync(50)).ReturnsAsync((Domain.Model.Representative?)null);

            // Act
            var result = await _service.DeactivateAsync(50);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeactivateAsync_WhenOnlyOneActiveRepresentative_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var rep = Mock.Of<Domain.Model.Representative>(r => r.OrganizationId == 1 && r.IsActive == true);
            _mockRepresentativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(rep);
            _mockRepresentativeRepository.Setup(r => r.GetByOrganizationIdAsync(1)).ReturnsAsync(new List<Domain.Model.Representative> { rep });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeactivateAsync(1));
        }

        [Fact]
        public async Task DeactivateAsync_WhenMultipleActiveRepresentatives_ShouldDeactivateAndDeactivateSystemUser()
        {
            // Arrange
            var domainRep = Mock.Of<Domain.Model.Representative>(r => r.OrganizationId == 2 && r.Email == "rep@org.com");
            _mockRepresentativeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(domainRep);

            var otherRep = Mock.Of<Domain.Model.Representative>(r => r.IsActive == true);
            _mockRepresentativeRepository.Setup(r => r.GetByOrganizationIdAsync(2)).ReturnsAsync(new List<Domain.Model.Representative> { domainRep, otherRep });

            _mockRepresentativeRepository.Setup(r => r.UpdateAsync(It.IsAny<Domain.Model.Representative>())).ReturnsAsync(domainRep);

            // existing system user active
            var existingUser = new SystemUserDTO { Email = "rep@org.com", Active = true, Role = "ShippingAgentRepresentative" };
            _mockUserManagementService.Setup(u => u.GetUserByEmailAsync("rep@org.com")).ReturnsAsync(existingUser);

            // Act
            var result = await _service.DeactivateAsync(1);

            // Assert
            Assert.True(result);
            _mockRepresentativeRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.Model.Representative>()), Times.Once);
            _mockUserManagementService.Verify(u => u.GetUserByEmailAsync("rep@org.com"), Times.Once);
            _mockUserManagementService.Verify(u => u.UpdateUserAsync(It.Is<UpdateSystemUserDTO>(dto => dto.Active == false)), Times.Once);
        }

        [Fact]
        public async Task ReactivateAsync_WhenRepresentativeNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockRepresentativeRepository.Setup(r => r.GetByIdAsync(60)).ReturnsAsync((Domain.Model.Representative?)null);

            // Act
            var result = await _service.ReactivateAsync(60);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReactivateAsync_WhenRepresentativeExists_ShouldReactivateAndReactivateSystemUser()
        {
            // Arrange
            var domainRep = Mock.Of<Domain.Model.Representative>(r => r.Email == "rep2@org.com");
            _mockRepresentativeRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(domainRep);
            _mockRepresentativeRepository.Setup(r => r.UpdateAsync(domainRep)).ReturnsAsync(domainRep);

            var existingUser = new SystemUserDTO { Email = "rep2@org.com", Active = false, Role = "ShippingAgentRepresentative" };
            _mockUserManagementService.Setup(u => u.GetUserByEmailAsync("rep2@org.com")).ReturnsAsync(existingUser);

            // Act
            var result = await _service.ReactivateAsync(2);

            // Assert
            Assert.True(result);
            _mockRepresentativeRepository.Verify(r => r.UpdateAsync(domainRep), Times.Once);
            _mockUserManagementService.Verify(u => u.GetUserByEmailAsync("rep2@org.com"), Times.Once);
            _mockUserManagementService.Verify(u => u.UpdateUserAsync(It.Is<UpdateSystemUserDTO>(dto => dto.Active == true)), Times.Once);
        }

        #endregion
    }
}*/