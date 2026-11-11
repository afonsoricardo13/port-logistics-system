using Application.DTO;
using System.Text.Json;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.7: Integration tests for Create Vessel Visit Execution functionality
/// Tests cover DTO validation, structure verification, and JSON serialization
/// </summary>
public class VesselVisitExecutionsControllerTests_US4_1_7
{
    #region CreateVveDTO Tests

    /// <summary>
    /// Test 1: CreateVveDTO should have all required properties
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow,
            Notes = "Test notes"
        };

        // Assert
        Assert.NotNull(dto.VvnId);
        Assert.NotNull(dto.ActualArrivalTime);
        Assert.NotNull(dto.Notes);
    }

    /// <summary>
    /// Test 2: CreateVveDTO should allow VvnId to be set
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldAllowVvnIdToBeSet()
    {
        // Arrange
        var expectedVvnId = "VVN-2026-001";

        // Act
        var dto = new CreateVveDTO
        {
            VvnId = expectedVvnId,
            ActualArrivalTime = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(expectedVvnId, dto.VvnId);
    }

    /// <summary>
    /// Test 3: CreateVveDTO should allow ActualArrivalTime to be set
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldAllowActualArrivalTimeToBeSet()
    {
        // Arrange
        var expectedTime = new DateTime(2026, 1, 15, 10, 30, 0);

        // Act
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = expectedTime
        };

        // Assert
        Assert.Equal(expectedTime, dto.ActualArrivalTime);
    }

    /// <summary>
    /// Test 4: CreateVveDTO should allow Notes to be optional
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldAllowNotesToBeOptional()
    {
        // Arrange & Act
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow,
            Notes = null
        };

        // Assert
        Assert.Null(dto.Notes);
    }

    /// <summary>
    /// Test 5: CreateVveDTO should allow ActualArrivalTime to be nullable
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldAllowActualArrivalTimeToBeNullable()
    {
        // Arrange & Act
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = null
        };

        // Assert
        Assert.Null(dto.ActualArrivalTime);
    }

    #endregion

    #region VesselVisitExecutionDTO Response Tests

    /// <summary>
    /// Test 6: VesselVisitExecutionDTO should have all expected properties
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ShouldHaveAllProperties()
    {
        // Arrange & Act
        var dto = new VesselVisitExecutionDTO
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow,
            ActualBerthTime = DateTime.UtcNow.AddHours(1),
            DockUsed = "Dock-A",
            ExecutedOperations = "[{\"type\":\"Loading\"}]",
            ActualUnberthTime = DateTime.UtcNow.AddHours(5),
            ActualDepartureTime = DateTime.UtcNow.AddHours(6),
            Status = "InProgress",
            CreatedBy = "user@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = "user2@example.com",
            UpdatedAt = DateTime.UtcNow,
            AuditLog = "[]",
            Notes = "Test notes"
        };

        // Assert
        Assert.NotNull(dto.Id);
        Assert.NotNull(dto.VvnId);
        Assert.NotNull(dto.ActualArrivalTime);
        Assert.NotNull(dto.ActualBerthTime);
        Assert.NotNull(dto.DockUsed);
        Assert.NotNull(dto.ExecutedOperations);
        Assert.NotNull(dto.ActualUnberthTime);
        Assert.NotNull(dto.ActualDepartureTime);
        Assert.NotNull(dto.Status);
        Assert.NotNull(dto.CreatedBy);
        Assert.NotNull(dto.CreatedAt);
        Assert.NotNull(dto.UpdatedBy);
        Assert.NotNull(dto.UpdatedAt);
        Assert.NotNull(dto.AuditLog);
        Assert.NotNull(dto.Notes);
    }

    /// <summary>
    /// Test 7: VesselVisitExecutionDTO should have default Status
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ShouldHaveDefaultStatus()
    {
        // Arrange & Act
        var dto = new VesselVisitExecutionDTO
        {
            VvnId = "VVN-2026-001",
            CreatedBy = "user@example.com"
        };

        // Assert
        Assert.Equal("InProgress", dto.Status);
    }

    /// <summary>
    /// Test 8: VesselVisitExecutionDTO should allow nullable optional fields
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ShouldAllowNullableOptionalFields()
    {
        // Arrange & Act
        var dto = new VesselVisitExecutionDTO
        {
            VvnId = "VVN-2026-001",
            CreatedBy = "user@example.com",
            Id = null,
            ActualArrivalTime = null,
            ActualBerthTime = null,
            DockUsed = null,
            ExecutedOperations = null,
            ActualUnberthTime = null,
            ActualDepartureTime = null,
            UpdatedBy = null,
            UpdatedAt = null,
            AuditLog = null,
            Notes = null
        };

        // Assert
        Assert.Null(dto.Id);
        Assert.Null(dto.ActualArrivalTime);
        Assert.Null(dto.ActualBerthTime);
        Assert.Null(dto.DockUsed);
        Assert.Null(dto.ExecutedOperations);
        Assert.Null(dto.ActualUnberthTime);
        Assert.Null(dto.ActualDepartureTime);
        Assert.Null(dto.UpdatedBy);
        Assert.Null(dto.UpdatedAt);
        Assert.Null(dto.AuditLog);
        Assert.Null(dto.Notes);
    }

    #endregion

    #region JSON Serialization Tests

    /// <summary>
    /// Test 9: CreateVveDTO should serialize to JSON correctly
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldSerializeToJson()
    {
        // Arrange
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Notes = "Test notes"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.Contains("\"VvnId\":", json);
        Assert.Contains("VVN-2026-001", json);
        Assert.Contains("\"ActualArrivalTime\":", json);
        Assert.Contains("\"Notes\":", json);
        Assert.Contains("Test notes", json);
    }

    /// <summary>
    /// Test 10: CreateVveDTO should deserialize from JSON correctly
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""VvnId"": ""VVN-2026-001"",
            ""ActualArrivalTime"": ""2026-01-15T10:30:00Z"",
            ""Notes"": ""Test notes""
        }";

        // Act
        var dto = JsonSerializer.Deserialize<CreateVveDTO>(json);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("VVN-2026-001", dto.VvnId);
        Assert.NotNull(dto.ActualArrivalTime);
        Assert.Equal("Test notes", dto.Notes);
    }

    /// <summary>
    /// Test 11: VesselVisitExecutionDTO should serialize to JSON correctly
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ShouldSerializeToJson()
    {
        // Arrange
        var dto = new VesselVisitExecutionDTO
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            ActualArrivalTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Status = "InProgress",
            CreatedBy = "user@example.com",
            CreatedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Notes = "Test notes"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.Contains("\"Id\":", json);
        Assert.Contains("VVE-001", json);
        Assert.Contains("\"VvnId\":", json);
        Assert.Contains("VVN-2026-001", json);
        Assert.Contains("\"Status\":", json);
        Assert.Contains("InProgress", json);
        Assert.Contains("\"CreatedBy\":", json);
        Assert.Contains("user@example.com", json);
    }

    /// <summary>
    /// Test 12: VesselVisitExecutionDTO should deserialize from JSON correctly
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""Id"": ""VVE-001"",
            ""VvnId"": ""VVN-2026-001"",
            ""ActualArrivalTime"": ""2026-01-15T10:30:00Z"",
            ""Status"": ""InProgress"",
            ""CreatedBy"": ""user@example.com"",
            ""CreatedAt"": ""2026-01-15T10:00:00Z"",
            ""Notes"": ""Test notes""
        }";

        // Act
        var dto = JsonSerializer.Deserialize<VesselVisitExecutionDTO>(json);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("VVE-001", dto.Id);
        Assert.Equal("VVN-2026-001", dto.VvnId);
        Assert.Equal("InProgress", dto.Status);
        Assert.Equal("user@example.com", dto.CreatedBy);
        Assert.Equal("Test notes", dto.Notes);
    }

    /// <summary>
    /// Test 13: CreateVveDTO should handle null values in JSON
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldHandleNullValuesInJson()
    {
        // Arrange
        var json = @"{
            ""VvnId"": ""VVN-2026-001"",
            ""ActualArrivalTime"": null,
            ""Notes"": null
        }";

        // Act
        var dto = JsonSerializer.Deserialize<CreateVveDTO>(json);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("VVN-2026-001", dto.VvnId);
        Assert.Null(dto.ActualArrivalTime);
        Assert.Null(dto.Notes);
    }

    /// <summary>
    /// Test 14: VesselVisitExecutionDTO should preserve data through serialization roundtrip
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ShouldPreserveDataThroughRoundtrip()
    {
        // Arrange
        var original = new VesselVisitExecutionDTO
        {
            Id = "VVE-001",
            VvnId = "VVN-2026-001",
            ActualArrivalTime = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            ActualBerthTime = new DateTime(2026, 1, 15, 11, 30, 0, DateTimeKind.Utc),
            DockUsed = "Dock-A",
            Status = "InProgress",
            CreatedBy = "user@example.com",
            CreatedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            Notes = "Test notes"
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<VesselVisitExecutionDTO>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.VvnId, deserialized.VvnId);
        Assert.Equal(original.Status, deserialized.Status);
        Assert.Equal(original.CreatedBy, deserialized.CreatedBy);
        Assert.Equal(original.Notes, deserialized.Notes);
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Test 15: CreateVveDTO should validate VvnId is required
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldValidateVvnIdRequired()
    {
        // Arrange
        var json = @"{
            ""ActualArrivalTime"": ""2026-01-15T10:30:00Z"",
            ""Notes"": ""Test notes""
        }";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateVveDTO>(json));
    }

    /// <summary>
    /// Test 16: VesselVisitExecutionDTO should validate VvnId is required
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ShouldValidateVvnIdRequired()
    {
        // Arrange
        var json = @"{
            ""Id"": ""VVE-001"",
            ""Status"": ""InProgress"",
            ""CreatedBy"": ""user@example.com""
        }";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<VesselVisitExecutionDTO>(json));
    }

    /// <summary>
    /// Test 17: VesselVisitExecutionDTO should validate CreatedBy is required
    /// </summary>
    [Fact]
    public void VesselVisitExecutionDTO_ShouldValidateCreatedByRequired()
    {
        // Arrange
        var json = @"{
            ""Id"": ""VVE-001"",
            ""VvnId"": ""VVN-2026-001"",
            ""Status"": ""InProgress""
        }";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<VesselVisitExecutionDTO>(json));
    }

    /// <summary>
    /// Test 18: CreateVveDTO should accept various VvnId formats
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldAcceptVariousVvnIdFormats()
    {
        // Arrange & Act
        var dto1 = new CreateVveDTO { VvnId = "VVN-2026-001", ActualArrivalTime = DateTime.UtcNow };
        var dto2 = new CreateVveDTO { VvnId = "VVN_2026_001", ActualArrivalTime = DateTime.UtcNow };
        var dto3 = new CreateVveDTO { VvnId = "VVN2026001", ActualArrivalTime = DateTime.UtcNow };

        // Assert
        Assert.Equal("VVN-2026-001", dto1.VvnId);
        Assert.Equal("VVN_2026_001", dto2.VvnId);
        Assert.Equal("VVN2026001", dto3.VvnId);
    }

    /// <summary>
    /// Test 19: CreateVveDTO should accept future arrival times
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldAcceptFutureArrivalTimes()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddDays(7);

        // Act
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = futureTime
        };

        // Assert
        Assert.Equal(futureTime, dto.ActualArrivalTime);
        Assert.True(dto.ActualArrivalTime > DateTime.UtcNow);
    }

    /// <summary>
    /// Test 20: CreateVveDTO should accept long notes text
    /// </summary>
    [Fact]
    public void CreateVveDTO_ShouldAcceptLongNotesText()
    {
        // Arrange
        var longNotes = new string('A', 1000);

        // Act
        var dto = new CreateVveDTO
        {
            VvnId = "VVN-2026-001",
            ActualArrivalTime = DateTime.UtcNow,
            Notes = longNotes
        };

        // Assert
        Assert.Equal(1000, dto.Notes.Length);
        Assert.Equal(longNotes, dto.Notes);
    }

    #endregion
}
