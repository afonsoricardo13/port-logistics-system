using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// Unit tests for QualificationService
/// US 2.2.13: Register and manage qualifications (create, update)
/// Tests validate service layer logic and integration with repository
/// </summary>
public class QualificationServiceTests
{
    private readonly Mock<IQualificationRepository> _mockRepository;
    private readonly QualificationService _service;

    public QualificationServiceTests()
    {
        _mockRepository = new Mock<IQualificationRepository>();
        _service = new QualificationService(_mockRepository.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenNoQualifications_ShouldReturnEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Qualification>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithQualifications_ShouldReturnAllQualifications()
    {
        // Arrange
        var qualifications = new List<Qualification>
        {
            new Qualification("STS Crane Operator", 5),
            new Qualification("Truck Driver", 10),
            new Qualification("Yard Planner", 3)
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(qualifications);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetByQualificationNameAsync Tests

    [Fact]
    public async Task GetByQualificationNameAsync_WhenQualificationExists_ShouldReturnQualification()
    {
        // Arrange
        var qualification = new Qualification("Crane Operator", 5);
        _mockRepository.Setup(r => r.GetByQualificationNameAsync("CRANE OPERATOR"))
            .ReturnsAsync(qualification);

        // Act
        var result = await _service.GetByQualificationNameAsync("CRANE OPERATOR");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CRANE OPERATOR", result.QualificationName);
        Assert.Equal(5, result.RequiredNumber);
        _mockRepository.Verify(r => r.GetByQualificationNameAsync("CRANE OPERATOR"), Times.Once);
    }

    [Fact]
    public async Task GetByQualificationNameAsync_WhenQualificationDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByQualificationNameAsync("NONEXISTENT"))
            .ReturnsAsync((Qualification?)null);

        // Act
        var result = await _service.GetByQualificationNameAsync("NONEXISTENT");

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByQualificationNameAsync("NONEXISTENT"), Times.Once);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNamePattern_ShouldReturnMatchingQualifications()
    {
        // Arrange
        var qualifications = new List<Qualification>
        {
            new Qualification("STS Crane Operator", 5),
            new Qualification("Yard Gantry Crane Operator", 3)
        };
        _mockRepository.Setup(r => r.SearchAsync("Crane")).ReturnsAsync(qualifications);

        // Act
        var result = await _service.SearchAsync("Crane");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.SearchAsync("Crane"), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithNullPattern_ShouldReturnAllQualifications()
    {
        // Arrange
        var qualifications = new List<Qualification>
        {
            new Qualification("Crane Operator", 5),
            new Qualification("Truck Driver", 8)
        };
        _mockRepository.Setup(r => r.SearchAsync(null)).ReturnsAsync(qualifications);

        // Act
        var result = await _service.SearchAsync(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.SearchAsync(null), Times.Once);
    }

    #endregion

    #region AddAsync Tests - Valid Cases

    [Fact]
    public async Task AddAsync_WithValidData_ShouldCreateQualification()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = "STS Crane Operator",
            RequiredNumber = 5
        };
        var savedQualification = new Qualification("STS Crane Operator", 5);

        _mockRepository.Setup(r => r.ExistsByQualificationNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Qualification>()))
            .ReturnsAsync(savedQualification);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("STS CRANE OPERATOR", result.QualificationName); // Normalized
        Assert.Equal(5, result.RequiredNumber);
        Assert.Empty(errorMessages);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Qualification>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_WithMinimumValidData_ShouldCreateQualification()
    {
        // Arrange - 2 characters minimum, 0 required number
        var dto = new CreateQualificationDTO
        {
            QualificationName = "IT",
            RequiredNumber = 0
        };
        var savedQualification = new Qualification("IT", 0);

        _mockRepository.Setup(r => r.ExistsByQualificationNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Qualification>()))
            .ReturnsAsync(savedQualification);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IT", result.QualificationName);
        Assert.Equal(0, result.RequiredNumber);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public async Task AddAsync_WithMaximumValidData_ShouldCreateQualification()
    {
        // Arrange - 100 characters maximum, 999 required number
        var longName = new string('A', 100);
        var dto = new CreateQualificationDTO
        {
            QualificationName = longName,
            RequiredNumber = 999
        };
        var savedQualification = new Qualification(longName, 999);

        _mockRepository.Setup(r => r.ExistsByQualificationNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Qualification>()))
            .ReturnsAsync(savedQualification);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.QualificationName.Length);
        Assert.Equal(999, result.RequiredNumber);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public async Task AddAsync_WithWhitespaceInName_ShouldTrimAndCreate()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = "  Truck Driver  ",
            RequiredNumber = 10
        };
        var savedQualification = new Qualification("Truck Driver", 10);

        _mockRepository.Setup(r => r.ExistsByQualificationNameAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Qualification>()))
            .ReturnsAsync(savedQualification);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TRUCK DRIVER", result.QualificationName); // Trimmed and normalized
        Assert.Empty(errorMessages);
    }

    #endregion

    #region AddAsync Tests - Invalid Cases

    [Fact]
    public async Task AddAsync_WithEmptyName_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = "",
            RequiredNumber = 5
        };
        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("cannot be empty", errorMessages[0]);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Qualification>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WithWhitespaceName_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = "   ",
            RequiredNumber = 5
        };
        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("cannot be empty", errorMessages[0]);
    }

    [Fact]
    public async Task AddAsync_WithSingleCharacterName_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = "A",
            RequiredNumber = 5
        };
        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("at least 2 characters", errorMessages[0]);
    }

    [Fact]
    public async Task AddAsync_WithNameExceeding100Characters_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = new string('B', 101),
            RequiredNumber = 5
        };
        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("cannot exceed 100 characters", errorMessages[0]);
    }

    [Fact]
    public async Task AddAsync_WithNegativeRequiredNumber_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = "Crane Operator",
            RequiredNumber = -1
        };
        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("non-negative", errorMessages[0]);
    }

    [Fact]
    public async Task AddAsync_WithRequiredNumberExceeding999_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = "Worker",
            RequiredNumber = 1000
        };
        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("cannot exceed 999", errorMessages[0]);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateName_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new CreateQualificationDTO
        {
            QualificationName = "Crane Operator",
            RequiredNumber = 5
        };
        _mockRepository.Setup(r => r.ExistsByQualificationNameAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.AddAsync(dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("already exists", errorMessages[0]);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Qualification>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests - Valid Cases

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateQualification()
    {
        // Arrange
        var existingQualification = new Qualification("Crane Operator", 5);
        var dto = new UpdateQualificationDTO { RequiredNumber = 10 };

        _mockRepository.Setup(r => r.GetByQualificationNameAsync("CRANE OPERATOR"))
            .ReturnsAsync(existingQualification);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Qualification>()))
            .ReturnsAsync(existingQualification);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("CRANE OPERATOR", dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.RequiredNumber);
        Assert.Empty(errorMessages);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Qualification>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ToZero_ShouldUpdateSuccessfully()
    {
        // Arrange
        var existingQualification = new Qualification("Safety Officer", 5);
        var dto = new UpdateQualificationDTO { RequiredNumber = 0 };

        _mockRepository.Setup(r => r.GetByQualificationNameAsync("SAFETY OFFICER"))
            .ReturnsAsync(existingQualification);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Qualification>()))
            .ReturnsAsync(existingQualification);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("SAFETY OFFICER", dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.RequiredNumber);
        Assert.Empty(errorMessages);
    }

    [Fact]
    public async Task UpdateAsync_ToMaximum_ShouldUpdateSuccessfully()
    {
        // Arrange
        var existingQualification = new Qualification("Worker", 50);
        var dto = new UpdateQualificationDTO { RequiredNumber = 999 };

        _mockRepository.Setup(r => r.GetByQualificationNameAsync("WORKER"))
            .ReturnsAsync(existingQualification);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Qualification>()))
            .ReturnsAsync(existingQualification);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("WORKER", dto, errorMessages);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(999, result.RequiredNumber);
        Assert.Empty(errorMessages);
    }

    #endregion

    #region UpdateAsync Tests - Invalid Cases

    [Fact]
    public async Task UpdateAsync_WithNegativeRequiredNumber_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new UpdateQualificationDTO { RequiredNumber = -1 };
        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("CRANE OPERATOR", dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("non-negative", errorMessages[0]);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Qualification>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithRequiredNumberExceeding999_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new UpdateQualificationDTO { RequiredNumber = 1000 };
        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("CRANE OPERATOR", dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("cannot exceed 999", errorMessages[0]);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentQualification_ShouldReturnNullAndAddError()
    {
        // Arrange
        var dto = new UpdateQualificationDTO { RequiredNumber = 10 };
        _mockRepository.Setup(r => r.GetByQualificationNameAsync("NONEXISTENT"))
            .ReturnsAsync((Qualification?)null);

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("NONEXISTENT", dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("not found", errorMessages[0]);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Qualification>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryReturnsNull_ShouldReturnNullAndAddError()
    {
        // Arrange
        var existingQualification = new Qualification("Crane Operator", 5);
        var dto = new UpdateQualificationDTO { RequiredNumber = 10 };

        _mockRepository.Setup(r => r.GetByQualificationNameAsync("CRANE OPERATOR"))
            .ReturnsAsync(existingQualification);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Qualification>()))
            .ReturnsAsync((Qualification?)null); // Simulate failure

        var errorMessages = new List<string>();

        // Act
        var result = await _service.UpdateAsync("CRANE OPERATOR", dto, errorMessages);

        // Assert
        Assert.Null(result);
        Assert.Single(errorMessages);
        Assert.Contains("Failed to update", errorMessages[0]);
    }

    #endregion

    #region ExistsByQualificationNameAsync Tests

    [Fact]
    public async Task ExistsByQualificationNameAsync_WhenQualificationExists_ShouldReturnTrue()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsByQualificationNameAsync("CRANE OPERATOR"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExistsByQualificationNameAsync("CRANE OPERATOR");

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.ExistsByQualificationNameAsync("CRANE OPERATOR"), Times.Once);
    }

    [Fact]
    public async Task ExistsByQualificationNameAsync_WhenQualificationDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsByQualificationNameAsync("NONEXISTENT"))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ExistsByQualificationNameAsync("NONEXISTENT");

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.ExistsByQualificationNameAsync("NONEXISTENT"), Times.Once);
    }

    #endregion
}
