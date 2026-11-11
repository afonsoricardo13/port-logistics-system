using Application.DTO;
using System.Text.Json;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.13: Integration tests for Incident Management DTOs
/// Tests verify DTO structure, JSON serialization, and data integrity
/// </summary>
public class IncidentsControllerTests_US4_1_13
{
    /// <summary>
    /// Test 1: CreateIncidentDTO should have IncidentTypeCode property
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldHaveIncidentTypeCodeProperty()
    {
        // Arrange & Act
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001"
        };

        // Assert
        Assert.Equal("T-INC001", dto.IncidentTypeCode);
    }

    /// <summary>
    /// Test 2: CreateIncidentDTO should have StartTime property
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldHaveStartTimeProperty()
    {
        // Arrange
        var startTime = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var dto = new CreateIncidentDTO
        {
            StartTime = startTime
        };

        // Assert
        Assert.Equal(startTime, dto.StartTime);
    }

    /// <summary>
    /// Test 3: CreateIncidentDTO should have Severity property
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldHaveSeverityProperty()
    {
        // Arrange & Act
        var dto = new CreateIncidentDTO
        {
            Severity = "Major"
        };

        // Assert
        Assert.Equal("Major", dto.Severity);
    }

    /// <summary>
    /// Test 4: CreateIncidentDTO should have Description property
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldHaveDescriptionProperty()
    {
        // Arrange & Act
        var dto = new CreateIncidentDTO
        {
            Description = "Equipment failure in cargo area"
        };

        // Assert
        Assert.Equal("Equipment failure in cargo area", dto.Description);
    }

    /// <summary>
    /// Test 5: CreateIncidentDTO should have CreatedBy property
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldHaveCreatedByProperty()
    {
        // Arrange & Act
        var dto = new CreateIncidentDTO
        {
            CreatedBy = "user@example.com"
        };

        // Assert
        Assert.Equal("user@example.com", dto.CreatedBy);
    }

    /// <summary>
    /// Test 6: IncidentDTO should have Id property
    /// </summary>
    [Fact]
    public void IncidentDTO_ShouldHaveIdProperty()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var dto = new IncidentDTO
        {
            Id = id
        };

        // Assert
        Assert.Equal(id, dto.Id);
    }

    /// <summary>
    /// Test 7: IncidentDTO should have Status property
    /// </summary>
    [Fact]
    public void IncidentDTO_ShouldHaveStatusProperty()
    {
        // Arrange & Act
        var dto = new IncidentDTO
        {
            Status = "Active"
        };

        // Assert
        Assert.Equal("Active", dto.Status);
    }

    /// <summary>
    /// Test 8: IncidentDTO should have AffectedVVEIds list property
    /// </summary>
    [Fact]
    public void IncidentDTO_ShouldHaveAffectedVVEIdsListProperty()
    {
        // Arrange & Act
        var dto = new IncidentDTO
        {
            AffectedVVEIds = new List<string> { "VVE-001", "VVE-002" }
        };

        // Assert
        Assert.NotNull(dto.AffectedVVEIds);
        Assert.Equal(2, dto.AffectedVVEIds.Count);
        Assert.Contains("VVE-001", dto.AffectedVVEIds);
        Assert.Contains("VVE-002", dto.AffectedVVEIds);
    }

    /// <summary>
    /// Test 9: CreateIncidentDTO should serialize to JSON correctly
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldSerializeToJson()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Severity = "Major",
            Description = "Test incident",
            CreatedBy = "user@example.com",
            AffectedVVEIds = new List<string> { "VVE-001" }
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.Contains("\"IncidentTypeCode\":\"T-INC001\"", json);
        Assert.Contains("\"Severity\":\"Major\"", json);
        Assert.Contains("\"Description\":\"Test incident\"", json);
        Assert.Contains("\"CreatedBy\":\"user@example.com\"", json);
    }

    /// <summary>
    /// Test 10: CreateIncidentDTO should deserialize from JSON correctly
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""IncidentTypeCode"": ""T-INC001"",
            ""StartTime"": ""2025-01-15T10:00:00Z"",
            ""Severity"": ""Major"",
            ""Description"": ""Test incident"",
            ""CreatedBy"": ""user@example.com"",
            ""AffectedVVEIds"": [""VVE-001""]
        }";

        // Act
        var dto = JsonSerializer.Deserialize<CreateIncidentDTO>(json);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("T-INC001", dto.IncidentTypeCode);
        Assert.Equal("Major", dto.Severity);
        Assert.Equal("Test incident", dto.Description);
        Assert.Equal("user@example.com", dto.CreatedBy);
    }

    /// <summary>
    /// Test 11: IncidentDTO should serialize to JSON correctly
    /// </summary>
    [Fact]
    public void IncidentDTO_ShouldSerializeToJson()
    {
        // Arrange
        var dto = new IncidentDTO
        {
            Id = Guid.NewGuid(),
            IncidentTypeCode = "T-INC001",
            StartTime = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Severity = "Major",
            Description = "Test incident",
            CreatedBy = "user@example.com",
            Status = "Active",
            AffectedVVEIds = new List<string> { "VVE-001" }
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.Contains("\"Id\":", json);
        Assert.Contains("\"IncidentTypeCode\":\"T-INC001\"", json);
        Assert.Contains("\"Status\":\"Active\"", json);
        Assert.Contains("\"Severity\":\"Major\"", json);
    }

    /// <summary>
    /// Test 12: IncidentDTO should deserialize from JSON correctly
    /// </summary>
    [Fact]
    public void IncidentDTO_ShouldDeserializeFromJson()
    {
        // Arrange
        var id = Guid.NewGuid();
        var json = $@"{{
            ""Id"": ""{id}"",
            ""IncidentTypeCode"": ""T-INC001"",
            ""StartTime"": ""2025-01-15T10:00:00Z"",
            ""Severity"": ""Major"",
            ""Description"": ""Test incident"",
            ""CreatedBy"": ""user@example.com"",
            ""Status"": ""Active"",
            ""AffectedVVEIds"": [""VVE-001""]
        }}";

        // Act
        var dto = JsonSerializer.Deserialize<IncidentDTO>(json);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(id, dto.Id);
        Assert.Equal("T-INC001", dto.IncidentTypeCode);
        Assert.Equal("Active", dto.Status);
        Assert.Equal("Major", dto.Severity);
    }

    /// <summary>
    /// Test 13: CreateIncidentDTO should serialize AffectedVVEIds as array
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldSerializeAffectedVVEIdsAsArray()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com",
            AffectedVVEIds = new List<string> { "VVE-001", "VVE-002", "VVE-003" }
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<CreateIncidentDTO>(json);

        // Assert
        Assert.NotNull(deserialized?.AffectedVVEIds);
        Assert.Equal(3, deserialized.AffectedVVEIds.Count);
        Assert.Contains("VVE-001", deserialized.AffectedVVEIds);
        Assert.Contains("VVE-002", deserialized.AffectedVVEIds);
        Assert.Contains("VVE-003", deserialized.AffectedVVEIds);
    }

    /// <summary>
    /// Test 14: CreateIncidentDTO should handle null AffectedVVEIds
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldHandleNullAffectedVVEIds()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com",
            AffectedVVEIds = null
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<CreateIncidentDTO>(json);

        // Assert
        Assert.NotNull(deserialized);
        // AffectedVVEIds can be null in DTO, service will convert to ["*"]
    }

    /// <summary>
    /// Test 15: IncidentDTO should have CreatedAt property
    /// </summary>
    [Fact]
    public void IncidentDTO_ShouldHaveCreatedAtProperty()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        // Act
        var dto = new IncidentDTO
        {
            CreatedAt = createdAt
        };

        // Assert
        Assert.Equal(createdAt, dto.CreatedAt);
    }

    /// <summary>
    /// Test 16: CreateIncidentDTO should validate required fields presence
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldHaveAllRequiredFields()
    {
        // Arrange & Act
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = DateTime.UtcNow,
            Severity = "Major",
            Description = "Test incident",
            CreatedBy = "user@example.com"
        };

        // Assert
        Assert.NotNull(dto.IncidentTypeCode);
        Assert.NotEqual(DateTime.MinValue, dto.StartTime);
        Assert.NotNull(dto.Severity);
        Assert.NotNull(dto.Description);
        Assert.NotNull(dto.CreatedBy);
    }

    /// <summary>
    /// Test 17: IncidentDTO should handle empty AffectedVVEIds list
    /// </summary>
    [Fact]
    public void IncidentDTO_ShouldHandleEmptyAffectedVVEIdsList()
    {
        // Arrange & Act
        var dto = new IncidentDTO
        {
            AffectedVVEIds = new List<string>()
        };

        // Assert
        Assert.NotNull(dto.AffectedVVEIds);
        Assert.Empty(dto.AffectedVVEIds);
    }

    /// <summary>
    /// Test 18: CreateIncidentDTO should serialize DateTime in ISO format
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldSerializeDateTimeInISOFormat()
    {
        // Arrange
        var dto = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = new DateTime(2025, 1, 15, 10, 30, 45, DateTimeKind.Utc),
            Severity = "Major",
            Description = "Test",
            CreatedBy = "user@example.com"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.Contains("2025-01-15T10:30:45", json);
    }

    /// <summary>
    /// Test 19: IncidentDTO should have IncidentTypeName property
    /// </summary>
    [Fact]
    public void IncidentDTO_ShouldHaveIncidentTypeNameProperty()
    {
        // Arrange & Act
        var dto = new IncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            IncidentTypeName = "Equipment Failure"
        };

        // Assert
        Assert.Equal("T-INC001", dto.IncidentTypeCode);
        Assert.Equal("Equipment Failure", dto.IncidentTypeName);
    }

    /// <summary>
    /// Test 20: CreateIncidentDTO should roundtrip through JSON serialization
    /// </summary>
    [Fact]
    public void CreateIncidentDTO_ShouldRoundtripThroughJsonSerialization()
    {
        // Arrange
        var original = new CreateIncidentDTO
        {
            IncidentTypeCode = "T-INC001",
            StartTime = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Severity = "Critical",
            Description = "Critical system failure",
            CreatedBy = "admin@example.com",
            AffectedVVEIds = new List<string> { "VVE-001", "VVE-002" }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<CreateIncidentDTO>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.IncidentTypeCode, deserialized.IncidentTypeCode);
        Assert.Equal(original.StartTime, deserialized.StartTime);
        Assert.Equal(original.Severity, deserialized.Severity);
        Assert.Equal(original.Description, deserialized.Description);
        Assert.Equal(original.CreatedBy, deserialized.CreatedBy);
        Assert.Equal(original.AffectedVVEIds?.Count, deserialized.AffectedVVEIds?.Count);
    }
}
