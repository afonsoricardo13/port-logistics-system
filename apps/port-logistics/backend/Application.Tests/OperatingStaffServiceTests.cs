using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// Unit tests for OperatingStaffService
/// US 2.2.11: Register and manage operating staff members (create, update, deactivate)
/// Tests validate service layer logic and integration with repository
/// </summary>
public class OperatingStaffServiceTests
{
    private readonly Mock<IOperatingStaffRepository> _mockStaffRepository;
    private readonly Mock<IQualificationRepository> _mockQualificationRepository;
    private readonly QualificationService _qualificationService;
    private readonly OperatingStaffService _service;

    public OperatingStaffServiceTests()
    {
        _mockStaffRepository = new Mock<IOperatingStaffRepository>();
        _mockQualificationRepository = new Mock<IQualificationRepository>();
        _qualificationService = new QualificationService(_mockQualificationRepository.Object);
        _service = new OperatingStaffService(_mockStaffRepository.Object, _qualificationService);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenNoStaff_ShouldReturnEmptyList()
    {
        // Arrange
        _mockStaffRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<OperatingStaff>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockStaffRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithStaff_ShouldReturnAllStaff()
    {
        // Arrange
        var staffList = new List<OperatingStaff>
        {
            CreateValidStaff("EMP001", "John Doe"),
            CreateValidStaff("EMP002", "Jane Smith"),
            CreateValidStaff("EMP003", "Bob Johnson")
        };
        _mockStaffRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(staffList);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockStaffRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetByMecanographicNumberAsync Tests

    [Fact]
    public async Task GetByMecanographicNumberAsync_WhenStaffExists_ShouldReturnStaff()
    {
        // Arrange
        var staff = CreateValidStaff("EMP001", "John Doe");
        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(staff);

        // Act
        var result = await _service.GetByMecanographicNumberAsync("EMP001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EMP001", result.MecanographicNumber);
        Assert.Equal("John Doe", result.Name);
        _mockStaffRepository.Verify(r => r.GetByMecanographicNumberAsync("EMP001"), Times.Once);
    }

    [Fact]
    public async Task GetByMecanographicNumberAsync_WhenStaffDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("NONEXISTENT"))
            .ReturnsAsync((OperatingStaff?)null);

        // Act
        var result = await _service.GetByMecanographicNumberAsync("NONEXISTENT");

        // Assert
        Assert.Null(result);
        _mockStaffRepository.Verify(r => r.GetByMecanographicNumberAsync("NONEXISTENT"), Times.Once);
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithValidStatus_ShouldReturnMatchingStaff()
    {
        // Arrange
        var availableStaff = new List<OperatingStaff>
        {
            CreateValidStaff("EMP001", "John Doe"),
            CreateValidStaff("EMP002", "Jane Smith")
        };
        _mockStaffRepository.Setup(r => r.GetByStatusAsync(StaffStatus.Available))
            .ReturnsAsync(availableStaff);

        // Act
        var result = await _service.GetByStatusAsync("Available");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockStaffRepository.Verify(r => r.GetByStatusAsync(StaffStatus.Available), Times.Once);
    }

    [Fact]
    public async Task GetByStatusAsync_WithInvalidStatus_ShouldReturnEmptyList()
    {
        // Arrange & Act
        var result = await _service.GetByStatusAsync("InvalidStatus");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockStaffRepository.Verify(r => r.GetByStatusAsync(It.IsAny<StaffStatus>()), Times.Never);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNameFilter_ShouldReturnMatchingStaff()
    {
        // Arrange
        var staffList = new List<OperatingStaff>
        {
            CreateValidStaff("EMP001", "John Doe")
        };
        _mockStaffRepository.Setup(r => r.SearchAsync("John", null, null, null))
            .ReturnsAsync(staffList);

        // Act
        var result = await _service.SearchAsync(name: "John");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("John Doe", result.First().Name);
    }

    [Fact]
    public async Task SearchAsync_WithMultipleFilters_ShouldReturnMatchingStaff()
    {
        // Arrange
        var staffList = new List<OperatingStaff>
        {
            CreateValidStaff("EMP001", "John Doe")
        };
        _mockStaffRepository.Setup(r => r.SearchAsync("John", "john@port.com", StaffStatus.Available, "Crane Operator"))
            .ReturnsAsync(staffList);

        // Act
        var result = await _service.SearchAsync("John", "john@port.com", "Available", "Crane Operator");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    #endregion

    #region AddAsync Tests - Valid Cases

    [Fact]
    public async Task AddAsync_WithValidData_ShouldCreateStaff()
    {
        // Arrange
        var dto = new CreateOperatingStaffDTO
        {
            MecanographicNumber = "EMP001",
            Name = "John Doe",
            Email = "john.doe@port.com",
            PhoneNumber = "+351912345678",
            OperationalWindow = "Mon-Fri 08:00-17:00",
            Qualifications = new List<string> { "Crane Operator" }
        };

        _mockStaffRepository.Setup(r => r.ExistsByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(false);
        _mockQualificationRepository.Setup(r => r.ExistsByQualificationNameAsync("Crane Operator"))
            .ReturnsAsync(true);
        _mockStaffRepository.Setup(r => r.AddAsync(It.IsAny<OperatingStaff>()))
            .ReturnsAsync((OperatingStaff s) => s);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EMP001", result.MecanographicNumber);
        Assert.Equal("John Doe", result.Name);
        Assert.Empty(errorMessages);
        _mockStaffRepository.Verify(r => r.AddAsync(It.IsAny<OperatingStaff>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithoutQualifications_ShouldCreateStaff()
    {
        // Arrange
        var dto = new CreateOperatingStaffDTO
        {
            MecanographicNumber = "EMP001",
            Name = "John Doe",
            Email = "john.doe@port.com",
            PhoneNumber = "+351912345678",
            OperationalWindow = "Mon-Fri 08:00-17:00",
            Qualifications = new List<string>()
        };

        _mockStaffRepository.Setup(r => r.ExistsByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(false);
        _mockStaffRepository.Setup(r => r.AddAsync(It.IsAny<OperatingStaff>()))
            .ReturnsAsync((OperatingStaff s) => s);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Qualifications);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public async Task AddAsync_WithMultipleQualifications_ShouldCreateStaff()
    {
        // Arrange
        var dto = new CreateOperatingStaffDTO
        {
            MecanographicNumber = "EMP001",
            Name = "John Doe",
            Email = "john.doe@port.com",
            PhoneNumber = "+351912345678",
            OperationalWindow = "Mon-Fri 08:00-17:00",
            Qualifications = new List<string> { "Crane Operator", "Safety Officer", "Forklift Driver" }
        };

        _mockStaffRepository.Setup(r => r.ExistsByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(false);
        _mockQualificationRepository.Setup(r => r.ExistsByQualificationNameAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockStaffRepository.Setup(r => r.AddAsync(It.IsAny<OperatingStaff>()))
            .ReturnsAsync((OperatingStaff s) => s);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Qualifications.Count);
        Assert.Empty(errorMessages);
    }

    #endregion

    #region AddAsync Tests - Invalid Cases

    [Fact]
    public async Task AddAsync_WithDuplicateMecanographicNumber_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateOperatingStaffDTO
        {
            MecanographicNumber = "EMP001",
            Name = "John Doe",
            Email = "john.doe@port.com",
            PhoneNumber = "+351912345678",
            OperationalWindow = "",
            Qualifications = new List<string>()
        };

        _mockStaffRepository.Setup(r => r.ExistsByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(true);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("already exists", errorMessages[0]);
        _mockStaffRepository.Verify(r => r.AddAsync(It.IsAny<OperatingStaff>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WithNonExistentQualification_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateOperatingStaffDTO
        {
            MecanographicNumber = "EMP001",
            Name = "John Doe",
            Email = "john.doe@port.com",
            PhoneNumber = "+351912345678",
            OperationalWindow = "",
            Qualifications = new List<string> { "NonExistentQualification" }
        };

        _mockStaffRepository.Setup(r => r.ExistsByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(false);
        _mockQualificationRepository.Setup(r => r.ExistsByQualificationNameAsync("NonExistentQualification"))
            .ReturnsAsync(false);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("does not exist", errorMessages[0]);
        _mockStaffRepository.Verify(r => r.AddAsync(It.IsAny<OperatingStaff>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests - Valid Cases

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateStaff()
    {
        // Arrange
        var existingStaff = CreateValidStaff("EMP001", "John Doe");
        var dto = new UpdateOperatingStaffDTO
        {
            Name = "John Updated",
            Email = "john.updated@port.com",
            PhoneNumber = "+351987654321",
            OperationalWindow = "Mon-Sun 00:00-23:59",
            Qualifications = new List<string> { "Safety Officer" }
        };

        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(existingStaff);
        _mockQualificationRepository.Setup(r => r.ExistsByQualificationNameAsync("Safety Officer"))
            .ReturnsAsync(true);
        _mockStaffRepository.Setup(r => r.UpdateAsync(It.IsAny<OperatingStaff>()))
            .ReturnsAsync((OperatingStaff s) => s);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("EMP001", dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Updated", result.Name);
        Assert.Equal("john.updated@port.com", result.Email);
        Assert.Empty(errorMessages);
        _mockStaffRepository.Verify(r => r.UpdateAsync(It.IsAny<OperatingStaff>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ChangingQualifications_ShouldUpdateCorrectly()
    {
        // Arrange
        var existingStaff = CreateValidStaff("EMP001", "John Doe");
        existingStaff.AddQualification("Old Qualification");

        var dto = new UpdateOperatingStaffDTO
        {
            Name = "John Doe",
            Email = "john.doe@port.com",
            PhoneNumber = "+351912345678",
            OperationalWindow = "Mon-Fri 08:00-17:00",
            Qualifications = new List<string> { "New Qualification" }
        };

        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(existingStaff);
        _mockQualificationRepository.Setup(r => r.ExistsByQualificationNameAsync("New Qualification"))
            .ReturnsAsync(true);
        _mockStaffRepository.Setup(r => r.UpdateAsync(It.IsAny<OperatingStaff>()))
            .ReturnsAsync((OperatingStaff s) => s);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("EMP001", dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("New Qualification", result.Qualifications);
        Assert.DoesNotContain("Old Qualification", result.Qualifications);
        Assert.Empty(errorMessages);
    }

    #endregion

    #region UpdateAsync Tests - Invalid Cases

    [Fact]
    public async Task UpdateAsync_WithNonExistentStaff_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new UpdateOperatingStaffDTO
        {
            Name = "John Doe",
            Email = "john.doe@port.com",
            PhoneNumber = "+351912345678",
            OperationalWindow = "",
            Qualifications = new List<string>()
        };

        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("NONEXISTENT"))
            .ReturnsAsync((OperatingStaff?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("NONEXISTENT", dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("not found", errorMessages[0]);
        _mockStaffRepository.Verify(r => r.UpdateAsync(It.IsAny<OperatingStaff>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentQualification_ShouldReturnNullAndAddError()
    {
        // Arrange
        var existingStaff = CreateValidStaff("EMP001", "John Doe");
        var dto = new UpdateOperatingStaffDTO
        {
            Name = "John Doe",
            Email = "john.doe@port.com",
            PhoneNumber = "+351912345678",
            OperationalWindow = "",
            Qualifications = new List<string> { "NonExistentQualification" }
        };

        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(existingStaff);
        _mockQualificationRepository.Setup(r => r.ExistsByQualificationNameAsync("NonExistentQualification"))
            .ReturnsAsync(false);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("EMP001", dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("does not exist", errorMessages[0]);
        _mockStaffRepository.Verify(r => r.UpdateAsync(It.IsAny<OperatingStaff>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryReturnsNull_ShouldReturnNullAndAddError()
    {
        // Arrange
        var existingStaff = CreateValidStaff("EMP001", "John Doe");
        var dto = new UpdateOperatingStaffDTO
        {
            Name = "John Updated",
            Email = "john.updated@port.com",
            PhoneNumber = "+351987654321",
            OperationalWindow = "Mon-Fri 08:00-17:00",
            Qualifications = new List<string>()
        };

        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(existingStaff);
        _mockStaffRepository.Setup(r => r.UpdateAsync(It.IsAny<OperatingStaff>()))
            .ReturnsAsync((OperatingStaff?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("EMP001", dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("Failed to update", errorMessages[0]);
    }

    #endregion

    #region DeactivateAsync Tests

    [Fact]
    public async Task DeactivateAsync_WithAvailableStaff_ShouldDeactivate()
    {
        // Arrange
        var staff = CreateValidStaff("EMP001", "John Doe");
        
        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(staff);
        _mockStaffRepository.Setup(r => r.UpdateAsync(It.IsAny<OperatingStaff>()))
            .ReturnsAsync((OperatingStaff s) => s);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.DeactivateAsync("EMP001", "Admin", "End of contract", errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Unavailable", result.Status);
        Assert.Empty(errorMessages);
        _mockStaffRepository.Verify(r => r.UpdateAsync(It.IsAny<OperatingStaff>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WithNonExistentStaff_ShouldReturnNullAndAddError()
    {
        // Arrange
        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("NONEXISTENT"))
            .ReturnsAsync((OperatingStaff?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.DeactivateAsync("NONEXISTENT", "Admin", "Reason", errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("not found", errorMessages[0]);
    }

    [Fact]
    public async Task DeactivateAsync_WithAlreadyUnavailableStaff_ShouldReturnNullAndAddError()
    {
        // Arrange
        var staff = CreateValidStaff("EMP001", "John Doe");
        staff.Deactivate("PreviousAdmin", "Already deactivated");

        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(staff);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.DeactivateAsync("EMP001", "Admin", "Reason", errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("already unavailable", errorMessages[0]);
    }

    #endregion

    #region ReactivateAsync Tests

    [Fact]
    public async Task ReactivateAsync_WithUnavailableStaff_ShouldReactivate()
    {
        // Arrange
        var staff = CreateValidStaff("EMP001", "John Doe");
        staff.Deactivate("Admin", "Temporary leave");

        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(staff);
        _mockStaffRepository.Setup(r => r.UpdateAsync(It.IsAny<OperatingStaff>()))
            .ReturnsAsync((OperatingStaff s) => s);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.ReactivateAsync("EMP001", "Admin", "Returned from leave", errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Available", result.Status);
        Assert.Empty(errorMessages);
        _mockStaffRepository.Verify(r => r.UpdateAsync(It.IsAny<OperatingStaff>()), Times.Once);
    }

    [Fact]
    public async Task ReactivateAsync_WithNonExistentStaff_ShouldReturnNullAndAddError()
    {
        // Arrange
        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("NONEXISTENT"))
            .ReturnsAsync((OperatingStaff?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.ReactivateAsync("NONEXISTENT", "Admin", "Reason", errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("not found", errorMessages[0]);
    }

    [Fact]
    public async Task ReactivateAsync_WithAvailableStaff_ShouldReturnNullAndAddError()
    {
        // Arrange
        var staff = CreateValidStaff("EMP001", "John Doe");

        _mockStaffRepository.Setup(r => r.GetByMecanographicNumberAsync("EMP001"))
            .ReturnsAsync(staff);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.ReactivateAsync("EMP001", "Admin", "Reason", errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("not unavailable", errorMessages[0]);
    }

    #endregion

    #region Helper Methods

    private OperatingStaff CreateValidStaff(string mecanographicNumber, string name)
    {
        return new OperatingStaff(
            mecanographicNumber,
            name,
            "john.doe@port.com",
            "+351912345678",
            "Mon-Fri 08:00-17:00",
            new List<string> { "Crane Operator" }
        );
    }

    #endregion
}
