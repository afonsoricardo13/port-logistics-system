using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

public class IncidentTypeServiceTests
{
    private readonly Mock<IIncidentTypeRepository> _mockRepo;
    private readonly IncidentTypeService _service;

    public IncidentTypeServiceTests()
    {
        _mockRepo = new Mock<IIncidentTypeRepository>();
        _service = new IncidentTypeService(_mockRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_ReturnsDTO()
    {
        // Arrange
        var dto = new CreateIncidentTypeDTO
        {
            Code = "T-INC001",
            Name = "Equipment Failure",
            Description = "Equipment malfunction",
            Severity = "Major"
        };

        _mockRepo.Setup(r => r.ExistsAsync(dto.Code)).ReturnsAsync(false);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<IncidentType>()))
            .ReturnsAsync((IncidentType it) => it);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(dto.Code, result.Code);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal("Major", result.Severity);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsException()
    {
        // Arrange
        var dto = new CreateIncidentTypeDTO { Code = "T-INC001", Name = "Test", Description = "Test", Severity = "Minor" };
        _mockRepo.Setup(r => r.ExistsAsync(dto.Code)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_InvalidSeverity_ThrowsException()
    {
        // Arrange
        var dto = new CreateIncidentTypeDTO { Code = "T-INC001", Name = "Test", Description = "Test", Severity = "Invalid" };
        _mockRepo.Setup(r => r.ExistsAsync(dto.Code)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_ValidData_ReturnsDTO()
    {
        // Arrange
        var code = "T-INC001";
        var existing = new IncidentType { Code = code, Name = "Old", Description = "Old", Severity = IncidentSeverity.Minor };
        var dto = new UpdateIncidentTypeDTO { Name = "Updated", Description = "Updated", Severity = "Major" };

        _mockRepo.Setup(r => r.GetByCodeAsync(code)).ReturnsAsync(existing);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<IncidentType>()))
            .ReturnsAsync((IncidentType it) => it);

        // Act
        var result = await _service.UpdateAsync(code, dto);

        // Assert
        Assert.Equal("Updated", result.Name);
        Assert.Equal("Major", result.Severity);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsException()
    {
        // Arrange
        var dto = new UpdateIncidentTypeDTO { Name = "Test", Description = "Test", Severity = "Minor" };
        _mockRepo.Setup(r => r.GetByCodeAsync("NOTFOUND")).ReturnsAsync((IncidentType?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync("NOTFOUND", dto));
    }

    [Fact]
    public async Task DeleteAsync_WithChildren_ThrowsException()
    {
        // Arrange
        var parent = new IncidentType
        {
            Code = "T-INC001",
            Name = "Parent",
            Description = "Parent",
            Severity = IncidentSeverity.Major,
            ChildTypes = new List<IncidentType>
            {
                new IncidentType { Code = "T-INC002", Name = "Child", Description = "Child", Severity = IncidentSeverity.Minor }
            }
        };

        _mockRepo.Setup(r => r.GetByCodeAsync("T-INC001")).ReturnsAsync(parent);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync("T-INC001"));
    }

    [Fact]
    public async Task DeleteAsync_NoChildren_ReturnsTrue()
    {
        // Arrange
        var type = new IncidentType { Code = "T-INC001", Name = "Test", Description = "Test", Severity = IncidentSeverity.Minor };
        _mockRepo.Setup(r => r.GetByCodeAsync("T-INC001")).ReturnsAsync(type);
        _mockRepo.Setup(r => r.DeleteAsync("T-INC001")).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync("T-INC001");

        // Assert
        Assert.True(result);
    }
}
