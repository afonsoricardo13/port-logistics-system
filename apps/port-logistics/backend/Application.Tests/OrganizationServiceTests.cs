/*            using System;
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
                public class OrganizationServiceTests
                {
                    private readonly Mock<IOrganizationRepository> _mockOrganizationRepository;
                    private readonly Mock<IRepresentativeRepository> _mockRepresentativeRepository;
                    private readonly Mock<IOrganizationFactory> _mockOrganizationFactory;
                    private readonly Mock<IRepresentativeFactory> _mockRepresentativeFactory;
                    private readonly Mock<IUserManagementService> _mockUserManagementService;
                    private readonly OrganizationService _service;
            
                    public OrganizationServiceTests()
                    {
                        _mockOrganizationRepository = new Mock<IOrganizationRepository>();
                        _mockRepresentativeRepository = new Mock<IRepresentativeRepository>();
                        _mockOrganizationFactory = new Mock<IOrganizationFactory>();
                        _mockRepresentativeFactory = new Mock<IRepresentativeFactory>();
                        _mockUserManagementService = new Mock<IUserManagementService>();
            
                        _service = new OrganizationService(
                            _mockOrganizationRepository.Object,
                            _mockRepresentativeRepository.Object,
                            _mockOrganizationFactory.Object,
                            _mockRepresentativeFactory.Object,
                            _mockUserManagementService.Object
                        );
                    }
            
                    #region GetByIdAsync Tests
            
                    [Fact]
                    public async Task GetByIdAsync_WhenOrganizationExists_ShouldReturnDto()
                    {
                        // Arrange
                        var domainOrg = Mock.Of<Domain.Model.Organization>();
                        _mockOrganizationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(domainOrg);
            
                        // Act
                        var result = await _service.GetByIdAsync(1);
            
                        // Assert
                        Assert.NotNull(result);
                        _mockOrganizationRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
                    }
            
                    [Fact]
                    public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
                    {
                        // Arrange
                        _mockOrganizationRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Domain.Model.Organization?)null);
            
                        // Act
                        var result = await _service.GetByIdAsync(999);
            
                        // Assert
                        Assert.Null(result);
                        _mockOrganizationRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
                    }
            
                    #endregion
            
                    #region GetByIdentifierAsync & GetAllAsync Tests
            
                    [Fact]
                    public async Task GetByIdentifierAsync_WhenOrganizationExists_ShouldReturnDto()
                    {
                        // Arrange
                        var domainOrg = Mock.Of<Domain.Model.Organization>();
                        _mockOrganizationRepository.Setup(r => r.GetByIdentifierAsync("ORG-123")).ReturnsAsync(domainOrg);
            
                        // Act
                        var result = await _service.GetByIdentifierAsync("ORG-123");
            
                        // Assert
                        Assert.NotNull(result);
                        _mockOrganizationRepository.Verify(r => r.GetByIdentifierAsync("ORG-123"), Times.Once);
                    }
            
                    [Fact]
                    public async Task GetAllAsync_WhenOrganizationsExist_ShouldReturnList()
                    {
                        // Arrange
                        var list = new List<Domain.Model.Organization>
                        {
                            Mock.Of<Domain.Model.Organization>(),
                            Mock.Of<Domain.Model.Organization>()
                        };
                        _mockOrganizationRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(list);
            
                        // Act
                        var result = await _service.GetAllAsync();
            
                        // Assert
                        Assert.NotNull(result);
                        Assert.Equal(2, result.Count());
                        _mockOrganizationRepository.Verify(r => r.GetAllAsync(), Times.Once);
                    }
            
                    #endregion
            
                    #region CreateAsync Tests
            
                    [Fact]
                    public async Task CreateAsync_WithUniqueData_ShouldCreateOrganization_AndCreateSystemUserWhenNotExists()
                    {
                        // Arrange
                        var createDto = new CreateOrganizationDTO
                        {
                            Identifier = "ORG-001",
                            LegalName = "Legal Name",
                            AlternativeName = null,
                            TaxNumber = "TAX-001",
                            Address = new AddressDTO {  },
                            InitialRepresentative = new CreateRepresentativeDTO
                            {
                                CitizenId = "CIT-001",
                                Name = "Rep Name",
                                Nationality = "PT",
                                Email = "rep@org.com",
                                PhoneNumber = "+351900000000"
                            }
                        };
            
                        _mockOrganizationRepository.Setup(r => r.ExistsByIdentifierAsync("ORG-001")).ReturnsAsync(false);
                        _mockOrganizationRepository.Setup(r => r.ExistsByTaxNumberAsync("TAX-001")).ReturnsAsync(false);
                        _mockRepresentativeRepository.Setup(r => r.ExistsByEmailAsync("rep@org.com")).ReturnsAsync(false);
                        _mockRepresentativeRepository.Setup(r => r.ExistsByCitizenIdAsync("CIT-001")).ReturnsAsync(false);
            
                        var createdDomainOrg = Mock.Of<Domain.Model.Organization>();
                        _mockRepresentativeFactory.Setup(f => f.CreateRepresentative(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                            .Returns(Mock.Of<Domain.Model.Representative>());
            
                        _mockOrganizationFactory.Setup(f => f.CreateOrganization(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Domain.Model.Address>(), It.IsAny<Domain.Model.Representative>()))
                            .Returns(createdDomainOrg);
            
                        _mockOrganizationRepository.Setup(r => r.AddAsync(createdDomainOrg)).ReturnsAsync(createdDomainOrg);
            
                        // return null => user does not exist
                        _mockUserManagementService.Setup(u => u.GetUserByEmailAsync("rep@org.com")).ReturnsAsync((SystemUserDTO?)null);
            
                        // Act
                        var result = await _service.CreateAsync(createDto);
            
                        // Assert
                        Assert.NotNull(result);
                        _mockOrganizationRepository.Verify(r => r.AddAsync(createdDomainOrg), Times.Once);
                        _mockUserManagementService.Verify(u => u.GetUserByEmailAsync("rep@org.com"), Times.Once);
                        _mockUserManagementService.Verify(u => u.CreateUserAsync(It.IsAny<CreateSystemUserDTO>(), "System"), Times.Once);
                    }
            
                    [Fact]
                    public async Task CreateAsync_WhenIdentifierAlreadyExists_ShouldThrowInvalidOperationException()
                    {
                        // Arrange
                        var createDto = new CreateOrganizationDTO { Identifier = "ORG-001", TaxNumber = "TAX-001", Address = new AddressDTO(), InitialRepresentative = new CreateRepresentativeDTO { Email = "rep@org.com", CitizenId = "CIT-001" } };
            
                        _mockOrganizationRepository.Setup(r => r.ExistsByIdentifierAsync("ORG-001")).ReturnsAsync(true);
            
                        // Act & Assert
                        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto));
                        _mockOrganizationRepository.Verify(r => r.ExistsByIdentifierAsync("ORG-001"), Times.Once);
                        _mockOrganizationRepository.Verify(r => r.AddAsync(It.IsAny<Domain.Model.Organization>()), Times.Never);
                    }
            
                    [Fact]
                    public async Task CreateAsync_WhenUserAlreadyExists_ShouldCallUpdateUser()
                    {
                        // Arrange
                        var createDto = new CreateOrganizationDTO
                        {
                            Identifier = "ORG-002",
                            LegalName = "Org 2",
                            TaxNumber = "TAX-002",
                            Address = new AddressDTO(),
                            InitialRepresentative = new CreateRepresentativeDTO
                            {
                                CitizenId = "CIT-002",
                                Name = "Rep 2",
                                Nationality = "PT",
                                Email = "rep2@org.com",
                                PhoneNumber = "+351900000001"
                            }
                        };
            
                        _mockOrganizationRepository.Setup(r => r.ExistsByIdentifierAsync("ORG-002")).ReturnsAsync(false);
                        _mockOrganizationRepository.Setup(r => r.ExistsByTaxNumberAsync("TAX-002")).ReturnsAsync(false);
                        _mockRepresentativeRepository.Setup(r => r.ExistsByEmailAsync("rep2@org.com")).ReturnsAsync(false);
                        _mockRepresentativeRepository.Setup(r => r.ExistsByCitizenIdAsync("CIT-002")).ReturnsAsync(false);
            
                        var createdDomainOrg = Mock.Of<Domain.Model.Organization>();
                        _mockRepresentativeFactory.Setup(f => f.CreateRepresentative(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                            .Returns(Mock.Of<Domain.Model.Representative>());
                        _mockOrganizationFactory.Setup(f => f.CreateOrganization(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Domain.Model.Address>(), It.IsAny<Domain.Model.Representative>()))
                            .Returns(createdDomainOrg);
                        _mockOrganizationRepository.Setup(r => r.AddAsync(createdDomainOrg)).ReturnsAsync(createdDomainOrg);
            
                        // existing system user -> return SystemUserDTO
                        _mockUserManagementService.Setup(u => u.GetUserByEmailAsync("rep2@org.com"))
                            .ReturnsAsync(new SystemUserDTO { Email = "rep2@org.com", Role = "SomeRole", Active = true });
            
                        // Act
                        var result = await _service.CreateAsync(createDto);
            
                        // Assert
                        Assert.NotNull(result);
                        _mockUserManagementService.Verify(u => u.GetUserByEmailAsync("rep2@org.com"), Times.Once);
                        _mockUserManagementService.Verify(u => u.UpdateUserAsync(It.IsAny<UpdateSystemUserDTO>()), Times.Once);
                    }
            
                    #endregion
            
                    #region UpdateAsync Tests
            
                    [Fact]
                    public async Task UpdateAsync_WhenOrganizationDoesNotExist_ShouldReturnNull()
                    {
                        // Arrange
                        _mockOrganizationRepository.Setup(r => r.GetByIdAsync(123)).ReturnsAsync((Domain.Model.Organization?)null);
                        var dto = new UpdateOrganizationDTO { Address = new AddressDTO() };
            
                        // Act
                        var result = await _service.UpdateAsync(123, dto);
            
                        // Assert
                        Assert.Null(result);
                        _mockOrganizationRepository.Verify(r => r.GetByIdAsync(123), Times.Once);
                        _mockOrganizationRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.Model.Organization>()), Times.Never);
                    }
            
                    [Fact]
                    public async Task UpdateAsync_WhenOrganizationExists_ShouldUpdateAndReturnDto()
                    {
                        // Arrange
                        var domainOrg = Mock.Of<Domain.Model.Organization>();
                        _mockOrganizationRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(domainOrg);
                        _mockOrganizationRepository.Setup(r => r.UpdateAsync(domainOrg)).ReturnsAsync(domainOrg);
                        var dto = new UpdateOrganizationDTO { Address = new AddressDTO(), LegalName = "New Name", AlternativeName = null, TaxNumber = "NEW-TAX" };
            
                        // Act
                        var result = await _service.UpdateAsync(10, dto);
            
                        // Assert
                        Assert.NotNull(result);
                        _mockOrganizationRepository.Verify(r => r.UpdateAsync(domainOrg), Times.Once);
                    }
            
                    #endregion
                }
            }
                */