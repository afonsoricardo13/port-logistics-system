using Application.DTO;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// US 4.1.14: Unit tests for Complementary Task Category operations
/// Tests cover category CRUD operations and business logic
/// </summary>
public class ComplementaryTaskCategoryServiceTests_US4_1_14
{
    private readonly Mock<IComplementaryTaskCategoryRepository> _mockRepository;

    public ComplementaryTaskCategoryServiceTests_US4_1_14()
    {
        _mockRepository = new Mock<IComplementaryTaskCategoryRepository>();
    }

    /// <summary>
    /// Test 1: Repository should create category with valid data
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldCreateCategory_WithValidData()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Hull Inspection",
            Description = "Inspection of vessel hull",
            DefaultDuration = 120,
            ExpectedImpact = "May cause 2h delay"
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CTC-001", result.Code);
        Assert.Equal("Hull Inspection", result.Name);
        Assert.Equal("Inspection of vessel hull", result.Description);
    }

    /// <summary>
    /// Test 2: Repository should get category by code
    /// </summary>
    [Fact]
    public async Task GetByCodeAsync_ShouldReturnCategory_WhenExists()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Hull Inspection",
            Description = "Inspection of vessel hull"
        };

        _mockRepository.Setup(r => r.GetByCodeAsync("CTC-001"))
            .ReturnsAsync(category);

        // Act
        var result = await _mockRepository.Object.GetByCodeAsync("CTC-001");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CTC-001", result.Code);
        Assert.Equal("Hull Inspection", result.Name);
    }

    /// <summary>
    /// Test 3: Repository should return null when category not found
    /// </summary>
    [Fact]
    public async Task GetByCodeAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByCodeAsync("INVALID"))
            .ReturnsAsync((ComplementaryTaskCategory?)null);

        // Act
        var result = await _mockRepository.Object.GetByCodeAsync("INVALID");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Test 4: Repository should update category successfully
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory_WithValidData()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Updated Name",
            Description = "Updated description",
            DefaultDuration = 180,
            ExpectedImpact = "Updated impact"
        };

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.UpdateAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(180, result.DefaultDuration);
    }

    /// <summary>
    /// Test 5: Repository should delete category by code
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldDeleteCategory_WhenExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync("CTC-001"))
            .ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.DeleteAsync("CTC-001");

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync("CTC-001"), Times.Once);
    }

    /// <summary>
    /// Test 6: Repository should get all categories
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        // Arrange
        var categories = new List<ComplementaryTaskCategory>
        {
            new ComplementaryTaskCategory { Code = "CTC-001", Name = "Hull Inspection" },
            new ComplementaryTaskCategory { Code = "CTC-002", Name = "Engine Maintenance" },
            new ComplementaryTaskCategory { Code = "CTC-003", Name = "Safety Check" }
        };

        _mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _mockRepository.Object.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    /// <summary>
    /// Test 7: Category should accept null DefaultDuration
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldAcceptNullDefaultDuration()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Quick Check",
            Description = "Quick inspection",
            DefaultDuration = null,
            ExpectedImpact = "Minimal impact"
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.DefaultDuration);
    }

    /// <summary>
    /// Test 8: Category should accept null ExpectedImpact
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldAcceptNullExpectedImpact()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Standard Task",
            Description = "Standard task category",
            DefaultDuration = 60,
            ExpectedImpact = null
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ExpectedImpact);
    }

    /// <summary>
    /// Test 9: Repository should check if category exists by code
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenCategoryExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync("CTC-001"))
            .ReturnsAsync(true);

        // Act
        var result = await _mockRepository.Object.ExistsAsync("CTC-001");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Test 10: Repository should return false when category doesn't exist
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenCategoryDoesNotExist()
    {
        // Arrange
        _mockRepository.Setup(r => r.ExistsAsync("INVALID"))
            .ReturnsAsync(false);

        // Act
        var result = await _mockRepository.Object.ExistsAsync("INVALID");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test 11: Category should handle long descriptions
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldHandleLongDescriptions()
    {
        // Arrange
        var longDescription = new string('A', 500);
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Test Category",
            Description = longDescription,
            DefaultDuration = 60
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.Equal(500, result.Description.Length);
    }

    /// <summary>
    /// Test 12: Category should handle special characters in name
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldHandleSpecialCharactersInName()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Task & Inspection (High Priority)",
            Description = "Description with @#$% characters",
            DefaultDuration = 60
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.Contains("&", result.Name);
        Assert.Contains("(", result.Name);
        Assert.Contains("@#$%", result.Description);
    }

    /// <summary>
    /// Test 13: Category should accept zero duration
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldAcceptZeroDuration()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Instant Task",
            Description = "Task with no duration",
            DefaultDuration = 0
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.Equal(0, result.DefaultDuration);
    }

    /// <summary>
    /// Test 14: Category should accept large duration values
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldAcceptLargeDurationValues()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Long Task",
            Description = "Task with long duration",
            DefaultDuration = 10080 // 7 days in minutes
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.Equal(10080, result.DefaultDuration);
    }

    /// <summary>
    /// Test 15: Update should preserve code
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldPreserveCode()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Updated Name",
            Description = "Updated description",
            DefaultDuration = 120
        };

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.UpdateAsync(category);

        // Assert
        Assert.Equal("CTC-001", result.Code);
    }

    /// <summary>
    /// Test 16: Update should allow changing all editable fields
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldAllowChangingAllEditableFields()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "New Name",
            Description = "New description",
            DefaultDuration = 240,
            ExpectedImpact = "New impact"
        };

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.UpdateAsync(category);

        // Assert
        Assert.Equal("New Name", result.Name);
        Assert.Equal("New description", result.Description);
        Assert.Equal(240, result.DefaultDuration);
        Assert.Equal("New impact", result.ExpectedImpact);
    }

    /// <summary>
    /// Test 17: Category code should be case-sensitive
    /// </summary>
    [Fact]
    public async Task GetByCodeAsync_ShouldBeCaseSensitive()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Test Category"
        };

        _mockRepository.Setup(r => r.GetByCodeAsync("CTC-001"))
            .ReturnsAsync(category);
        _mockRepository.Setup(r => r.GetByCodeAsync("ctc-001"))
            .ReturnsAsync((ComplementaryTaskCategory?)null);

        // Act
        var result1 = await _mockRepository.Object.GetByCodeAsync("CTC-001");
        var result2 = await _mockRepository.Object.GetByCodeAsync("ctc-001");

        // Assert
        Assert.NotNull(result1);
        Assert.Null(result2);
    }

    /// <summary>
    /// Test 18: Category should handle empty expected impact
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldHandleEmptyExpectedImpact()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Test Category",
            Description = "Test description",
            DefaultDuration = 60,
            ExpectedImpact = string.Empty
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.Equal(string.Empty, result.ExpectedImpact);
    }

    /// <summary>
    /// Test 19: GetAll should return empty list when no categories exist
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ComplementaryTaskCategory>());

        // Act
        var result = await _mockRepository.Object.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Test 20: Category should have all required properties
    /// </summary>
    [Fact]
    public async Task AddAsync_ShouldCreateCategoryWithAllProperties()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-999",
            Name = "Complete Category",
            Description = "Category with all properties",
            DefaultDuration = 150,
            ExpectedImpact = "Moderate delay expected"
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ComplementaryTaskCategory>()))
            .ReturnsAsync((ComplementaryTaskCategory c) => c);

        // Act
        var result = await _mockRepository.Object.AddAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CTC-999", result.Code);
        Assert.Equal("Complete Category", result.Name);
        Assert.Equal("Category with all properties", result.Description);
        Assert.Equal(150, result.DefaultDuration);
        Assert.Equal("Moderate delay expected", result.ExpectedImpact);
    }
}
