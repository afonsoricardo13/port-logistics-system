using Application.DTO;
using Domain.Model;
using System.Text.Json;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.14: Integration tests for Complementary Task Category Management DTOs
/// Tests verify DTO structure, JSON serialization, and data integrity
/// </summary>
public class ComplementaryTaskCategoriesControllerTests_US4_1_14
{
    /// <summary>
    /// Test 1: CreateComplementaryTaskCategoryDTO should have Code property
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldHaveCodeProperty()
    {
        // Arrange & Act
        var dto = new CreateComplementaryTaskCategoryDTO
        {
            Code = "CTC-001"
        };

        // Assert
        Assert.Equal("CTC-001", dto.Code);
    }

    /// <summary>
    /// Test 2: CreateComplementaryTaskCategoryDTO should have Name property
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldHaveNameProperty()
    {
        // Arrange & Act
        var dto = new CreateComplementaryTaskCategoryDTO
        {
            Name = "Hull Inspection"
        };

        // Assert
        Assert.Equal("Hull Inspection", dto.Name);
    }

    /// <summary>
    /// Test 3: CreateComplementaryTaskCategoryDTO should have Description property
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldHaveDescriptionProperty()
    {
        // Arrange & Act
        var dto = new CreateComplementaryTaskCategoryDTO
        {
            Description = "Inspection of vessel hull integrity"
        };

        // Assert
        Assert.Equal("Inspection of vessel hull integrity", dto.Description);
    }

    /// <summary>
    /// Test 4: CreateComplementaryTaskCategoryDTO should have DefaultDuration property
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldHaveDefaultDurationProperty()
    {
        // Arrange & Act
        var dto = new CreateComplementaryTaskCategoryDTO
        {
            DefaultDuration = 120
        };

        // Assert
        Assert.Equal(120, dto.DefaultDuration);
    }

    /// <summary>
    /// Test 5: CreateComplementaryTaskCategoryDTO should have ExpectedImpact property
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldHaveExpectedImpactProperty()
    {
        // Arrange & Act
        var dto = new CreateComplementaryTaskCategoryDTO
        {
            ExpectedImpact = "May cause 2h delay"
        };

        // Assert
        Assert.Equal("May cause 2h delay", dto.ExpectedImpact);
    }

    /// <summary>
    /// Test 6: ComplementaryTaskCategoryDTO should have Id property
    /// </summary>
    [Fact]
    public void ComplementaryTaskCategoryDTO_ShouldHaveIdProperty()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new ComplementaryTaskCategoryDTO
        {
            Id = id
        };

        // Assert
        Assert.Equal(id, dto.Id);
    }

    /// <summary>
    /// Test 7: ComplementaryTaskCategoryDTO should have Code property
    /// </summary>
    [Fact]
    public void ComplementaryTaskCategoryDTO_ShouldHaveCodeProperty()
    {
        // Arrange & Act
        var dto = new ComplementaryTaskCategoryDTO
        {
            Code = "CTC-001"
        };

        // Assert
        Assert.Equal("CTC-001", dto.Code);
    }

    /// <summary>
    /// Test 8: ComplementaryTaskCategoryDTO should have audit fields
    /// </summary>
    [Fact]
    public void ComplementaryTaskCategoryDTO_ShouldHaveAuditFields()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow.AddHours(1);

        // Act
        var dto = new ComplementaryTaskCategoryDTO
        {
            CreatedAt = createdAt,
            CreatedBy = "admin@example.com",
            UpdatedAt = updatedAt,
            UpdatedBy = "editor@example.com"
        };

        // Assert
        Assert.Equal(createdAt, dto.CreatedAt);
        Assert.Equal("admin@example.com", dto.CreatedBy);
        Assert.Equal(updatedAt, dto.UpdatedAt);
        Assert.Equal("editor@example.com", dto.UpdatedBy);
    }

    /// <summary>
    /// Test 9: UpdateComplementaryTaskCategoryDTO should have Name property
    /// </summary>
    [Fact]
    public void UpdateComplementaryTaskCategoryDTO_ShouldHaveNameProperty()
    {
        // Arrange & Act
        var dto = new UpdateComplementaryTaskCategoryDTO
        {
            Name = "Updated Name"
        };

        // Assert
        Assert.Equal("Updated Name", dto.Name);
    }

    /// <summary>
    /// Test 10: UpdateComplementaryTaskCategoryDTO should have Description property
    /// </summary>
    [Fact]
    public void UpdateComplementaryTaskCategoryDTO_ShouldHaveDescriptionProperty()
    {
        // Arrange & Act
        var dto = new UpdateComplementaryTaskCategoryDTO
        {
            Description = "Updated description"
        };

        // Assert
        Assert.Equal("Updated description", dto.Description);
    }

    /// <summary>
    /// Test 11: CreateComplementaryTaskCategoryDTO should serialize to JSON
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldSerializeToJson()
    {
        // Arrange
        var dto = new CreateComplementaryTaskCategoryDTO
        {
            Code = "CTC-001",
            Name = "Hull Inspection",
            Description = "Vessel hull inspection",
            DefaultDuration = 120,
            ExpectedImpact = "2h delay"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.Contains("\"Code\":\"CTC-001\"", json);
        Assert.Contains("\"Name\":\"Hull Inspection\"", json);
        Assert.Contains("\"Description\":\"Vessel hull inspection\"", json);
        Assert.Contains("\"DefaultDuration\":120", json);
    }

    /// <summary>
    /// Test 12: CreateComplementaryTaskCategoryDTO should deserialize from JSON
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""Code"": ""CTC-001"",
            ""Name"": ""Hull Inspection"",
            ""Description"": ""Vessel hull inspection"",
            ""DefaultDuration"": 120,
            ""ExpectedImpact"": ""2h delay""
        }";

        // Act
        var dto = JsonSerializer.Deserialize<CreateComplementaryTaskCategoryDTO>(json);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("CTC-001", dto.Code);
        Assert.Equal("Hull Inspection", dto.Name);
        Assert.Equal(120, dto.DefaultDuration);
    }

    /// <summary>
    /// Test 13: ComplementaryTaskCategoryDTO should serialize to JSON
    /// </summary>
    [Fact]
    public void ComplementaryTaskCategoryDTO_ShouldSerializeToJson()
    {
        // Arrange
        var dto = new ComplementaryTaskCategoryDTO
        {
            Id = Guid.NewGuid(),
            Code = "CTC-001",
            Name = "Hull Inspection",
            Description = "Vessel hull inspection",
            DefaultDuration = 120,
            ExpectedImpact = "2h delay",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin@example.com"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.Contains("\"Id\":", json);
        Assert.Contains("\"Code\":\"CTC-001\"", json);
        Assert.Contains("\"Name\":\"Hull Inspection\"", json);
        Assert.Contains("\"CreatedBy\":\"admin@example.com\"", json);
    }

    /// <summary>
    /// Test 14: ComplementaryTaskCategoryDTO should deserialize from JSON
    /// </summary>
    [Fact]
    public void ComplementaryTaskCategoryDTO_ShouldDeserializeFromJson()
    {
        // Arrange
        var id = Guid.NewGuid();
        var json = $@"{{
            ""Id"": ""{id}"",
            ""Code"": ""CTC-001"",
            ""Name"": ""Hull Inspection"",
            ""Description"": ""Vessel hull inspection"",
            ""DefaultDuration"": 120,
            ""ExpectedImpact"": ""2h delay"",
            ""CreatedAt"": ""2025-01-15T10:00:00Z"",
            ""CreatedBy"": ""admin@example.com""
        }}";

        // Act
        var dto = JsonSerializer.Deserialize<ComplementaryTaskCategoryDTO>(json);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(id, dto.Id);
        Assert.Equal("CTC-001", dto.Code);
        Assert.Equal("Hull Inspection", dto.Name);
        Assert.Equal("admin@example.com", dto.CreatedBy);
    }

    /// <summary>
    /// Test 15: UpdateComplementaryTaskCategoryDTO should serialize to JSON
    /// </summary>
    [Fact]
    public void UpdateComplementaryTaskCategoryDTO_ShouldSerializeToJson()
    {
        // Arrange
        var dto = new UpdateComplementaryTaskCategoryDTO
        {
            Name = "Updated Name",
            Description = "Updated description",
            DefaultDuration = 180,
            ExpectedImpact = "3h delay"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.Contains("\"Name\":\"Updated Name\"", json);
        Assert.Contains("\"Description\":\"Updated description\"", json);
        Assert.Contains("\"DefaultDuration\":180", json);
    }

    /// <summary>
    /// Test 16: CreateComplementaryTaskCategoryDTO should handle null optional fields
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldHandleNullOptionalFields()
    {
        // Arrange
        var dto = new CreateComplementaryTaskCategoryDTO
        {
            Code = "CTC-001",
            Name = "Test Category",
            Description = "Test description",
            DefaultDuration = null,
            ExpectedImpact = null
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<CreateComplementaryTaskCategoryDTO>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.DefaultDuration);
        Assert.Null(deserialized.ExpectedImpact);
    }

    /// <summary>
    /// Test 17: ComplementaryTaskCategory domain model should have Code property
    /// </summary>
    [Fact]
    public void ComplementaryTaskCategory_ShouldHaveCodeProperty()
    {
        // Arrange & Act
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001"
        };

        // Assert
        Assert.Equal("CTC-001", category.Code);
    }

    /// <summary>
    /// Test 18: ComplementaryTaskCategory should serialize to JSON
    /// </summary>
    [Fact]
    public void ComplementaryTaskCategory_ShouldSerializeToJson()
    {
        // Arrange
        var category = new ComplementaryTaskCategory
        {
            Code = "CTC-001",
            Name = "Hull Inspection",
            Description = "Vessel hull inspection",
            DefaultDuration = 120,
            ExpectedImpact = "2h delay"
        };

        // Act
        var json = JsonSerializer.Serialize(category);

        // Assert
        Assert.Contains("\"Code\":\"CTC-001\"", json);
        Assert.Contains("\"Name\":\"Hull Inspection\"", json);
        Assert.Contains("\"DefaultDuration\":120", json);
    }

    /// <summary>
    /// Test 19: CreateComplementaryTaskCategoryDTO should roundtrip through JSON
    /// </summary>
    [Fact]
    public void CreateComplementaryTaskCategoryDTO_ShouldRoundtripThroughJson()
    {
        // Arrange
        var original = new CreateComplementaryTaskCategoryDTO
        {
            Code = "CTC-999",
            Name = "Complete Category",
            Description = "Category with all properties",
            DefaultDuration = 150,
            ExpectedImpact = "Moderate delay"
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<CreateComplementaryTaskCategoryDTO>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Code, deserialized.Code);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Description, deserialized.Description);
        Assert.Equal(original.DefaultDuration, deserialized.DefaultDuration);
        Assert.Equal(original.ExpectedImpact, deserialized.ExpectedImpact);
    }

    /// <summary>
    /// Test 20: UpdateComplementaryTaskCategoryDTO should handle all fields
    /// </summary>
    [Fact]
    public void UpdateComplementaryTaskCategoryDTO_ShouldHaveAllEditableFields()
    {
        // Arrange & Act
        var dto = new UpdateComplementaryTaskCategoryDTO
        {
            Name = "Updated Name",
            Description = "Updated description",
            DefaultDuration = 240,
            ExpectedImpact = "High impact"
        };

        // Assert
        Assert.Equal("Updated Name", dto.Name);
        Assert.Equal("Updated description", dto.Description);
        Assert.Equal(240, dto.DefaultDuration);
        Assert.Equal("High impact", dto.ExpectedImpact);
    }
}
