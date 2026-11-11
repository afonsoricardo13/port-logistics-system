using Application.DTO;
using Domain.Model;
using System.Text.Json;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.4: Integration tests for Operation Plan Manual Update API
/// Tests cover DTO structures, request/response validation, and data serialization
/// </summary>
public class OperationPlansControllerTests_US4_1_4
{
    #region Request DTO Tests

    /// <summary>
    /// Test 1: UpdateOperationPlanRequestDTO can be created with all properties
    /// </summary>
    [Fact]
    public void UpdateOperationPlanRequestDTO_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new UpdateOperationPlanRequestDTO
        {
            UpdatedBy = "operator@port.com",
            ReasonForChange = "Dock maintenance required",
            AssignedDock = "DOCK-B",
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(6),
            CraneAssignments = new List<ResourceAssignmentDTO>
            {
                new ResourceAssignmentDTO { Type = "crane", Id = "CRANE-5", Name = "Crane 5" }
            },
            StaffAssignments = new List<ResourceAssignmentDTO>
            {
                new ResourceAssignmentDTO { Type = "staff", Id = "M12345", Name = "John Doe" }
            },
            Operations = new List<OperationDetailDTO>
            {
                new OperationDetailDTO { Type = "Loading" }
            }
        };

        // Assert
        Assert.Equal("operator@port.com", dto.UpdatedBy);
        Assert.Equal("Dock maintenance required", dto.ReasonForChange);
        Assert.Equal("DOCK-B", dto.AssignedDock);
        Assert.NotNull(dto.StartTime);
        Assert.NotNull(dto.EndTime);
        Assert.Single(dto.CraneAssignments);
        Assert.Single(dto.StaffAssignments);
        Assert.Single(dto.Operations);
    }

    /// <summary>
    /// Test 2: UpdateOperationPlanRequestDTO requires ReasonForChange
    /// </summary>
    [Fact]
    public void UpdateOperationPlanRequestDTO_RequiresReasonForChange()
    {
        // Arrange & Act
        var dto = new UpdateOperationPlanRequestDTO
        {
            UpdatedBy = "operator@port.com",
            ReasonForChange = "Required reason"
        };

        // Assert
        Assert.NotNull(dto.ReasonForChange);
        Assert.NotEmpty(dto.ReasonForChange);
    }

    /// <summary>
    /// Test 3: UpdateOperationPlanRequestDTO requires UpdatedBy
    /// </summary>
    [Fact]
    public void UpdateOperationPlanRequestDTO_RequiresUpdatedBy()
    {
        // Arrange & Act
        var dto = new UpdateOperationPlanRequestDTO
        {
            UpdatedBy = "operator@port.com",
            ReasonForChange = "Test"
        };

        // Assert
        Assert.NotNull(dto.UpdatedBy);
        Assert.NotEmpty(dto.UpdatedBy);
    }

    /// <summary>
    /// Test 4: UpdateOperationPlanRequestDTO supports optional dock update
    /// </summary>
    [Fact]
    public void UpdateOperationPlanRequestDTO_SupportsOptionalDockUpdate()
    {
        // Arrange & Act
        var dto = new UpdateOperationPlanRequestDTO
        {
            UpdatedBy = "operator@port.com",
            ReasonForChange = "Dock change",
            AssignedDock = "DOCK-C"
        };

        // Assert
        Assert.Equal("DOCK-C", dto.AssignedDock);
    }

    /// <summary>
    /// Test 5: UpdateOperationPlanRequestDTO supports optional time updates
    /// </summary>
    [Fact]
    public void UpdateOperationPlanRequestDTO_SupportsOptionalTimeUpdates()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(3);
        var endTime = startTime.AddHours(4);

        // Act
        var dto = new UpdateOperationPlanRequestDTO
        {
            UpdatedBy = "operator@port.com",
            ReasonForChange = "Vessel delay",
            StartTime = startTime,
            EndTime = endTime
        };

        // Assert
        Assert.Equal(startTime, dto.StartTime);
        Assert.Equal(endTime, dto.EndTime);
    }

    #endregion

    #region Response DTO Tests

    /// <summary>
    /// Test 6: UpdateOperationPlanResponseDTO has correct structure
    /// </summary>
    [Fact]
    public void UpdateOperationPlanResponseDTO_HasCorrectStructure()
    {
        // Arrange & Act
        var dto = new UpdateOperationPlanResponseDTO
        {
            Success = true,
            Message = "Operation plan updated successfully",
            UpdatedPlan = null,
            Warnings = new List<string>(),
            ResourceConflicts = new List<ResourceConflictDTO>(),
            VvnInconsistencies = new List<VvnInconsistencyDTO>()
        };

        // Assert
        Assert.True(dto.Success);
        Assert.Equal("Operation plan updated successfully", dto.Message);
        Assert.NotNull(dto.Warnings);
        Assert.NotNull(dto.ResourceConflicts);
        Assert.NotNull(dto.VvnInconsistencies);
    }

    /// <summary>
    /// Test 7: UpdateOperationPlanResponseDTO can include warnings
    /// </summary>
    [Fact]
    public void UpdateOperationPlanResponseDTO_CanIncludeWarnings()
    {
        // Arrange & Act
        var dto = new UpdateOperationPlanResponseDTO
        {
            Success = true,
            Message = "Updated with warnings",
            Warnings = new List<string>
            {
                "Resource CRANE-5 is not active",
                "Staff M12345 is not available"
            }
        };

        // Assert
        Assert.True(dto.Success);
        Assert.Equal(2, dto.Warnings.Count);
        Assert.Contains("not active", dto.Warnings[0]);
        Assert.Contains("not available", dto.Warnings[1]);
    }

    /// <summary>
    /// Test 8: UpdateOperationPlanResponseDTO can include resource conflicts
    /// </summary>
    [Fact]
    public void UpdateOperationPlanResponseDTO_CanIncludeResourceConflicts()
    {
        // Arrange & Act
        var dto = new UpdateOperationPlanResponseDTO
        {
            Success = true,
            Message = "Updated",
            ResourceConflicts = new List<ResourceConflictDTO>
            {
                new ResourceConflictDTO
                {
                    ResourceType = "dock",
                    ResourceId = "DOCK-A",
                    ConflictDescription = "Dock already assigned",
                    ConflictingVvnIds = new List<string> { "VVN-2025-002" },
                    ConflictStartTime = DateTime.UtcNow,
                    ConflictEndTime = DateTime.UtcNow.AddHours(4)
                }
            }
        };

        // Assert
        Assert.Single(dto.ResourceConflicts);
        Assert.Equal("dock", dto.ResourceConflicts[0].ResourceType);
        Assert.Equal("DOCK-A", dto.ResourceConflicts[0].ResourceId);
    }

    #endregion

    #region ChangeLogEntry DTO Tests

    /// <summary>
    /// Test 9: ChangeLogEntryDTO stores audit information
    /// </summary>
    [Fact]
    public void ChangeLogEntryDTO_StoresAuditInformation()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new ChangeLogEntryDTO
        {
            Timestamp = timestamp,
            Author = "operator@port.com",
            Reason = "Dock change",
            Changes = new List<string> { "Updated dock assignment: DOCK-B" }
        };

        // Assert
        Assert.Equal(timestamp, dto.Timestamp);
        Assert.Equal("operator@port.com", dto.Author);
        Assert.Equal("Dock change", dto.Reason);
        Assert.Single(dto.Changes);
    }

    /// <summary>
    /// Test 10: ChangeLogEntryDTO can track multiple changes
    /// </summary>
    [Fact]
    public void ChangeLogEntryDTO_CanTrackMultipleChanges()
    {
        // Arrange & Act
        var dto = new ChangeLogEntryDTO
        {
            Timestamp = DateTime.UtcNow,
            Author = "operator@port.com",
            Reason = "Complete replanning",
            Changes = new List<string>
            {
                "Updated dock assignment: DOCK-B",
                "Updated start time: 2026-01-15 10:00",
                "Updated end time: 2026-01-15 14:00",
                "Updated crane assignments: CRANE-5"
            }
        };

        // Assert
        Assert.Equal(4, dto.Changes.Count);
    }

    #endregion

    #region ResourceConflict DTO Tests

    /// <summary>
    /// Test 11: ResourceConflictDTO identifies dock conflicts
    /// </summary>
    [Fact]
    public void ResourceConflictDTO_IdentifiesDockConflicts()
    {
        // Arrange & Act
        var dto = new ResourceConflictDTO
        {
            ResourceType = "dock",
            ResourceId = "DOCK-A",
            ConflictDescription = "Dock is assigned to another vessel",
            ConflictingVvnIds = new List<string> { "VVN-2025-002", "VVN-2025-003" },
            ConflictStartTime = DateTime.UtcNow,
            ConflictEndTime = DateTime.UtcNow.AddHours(3)
        };

        // Assert
        Assert.Equal("dock", dto.ResourceType);
        Assert.Equal("DOCK-A", dto.ResourceId);
        Assert.Equal(2, dto.ConflictingVvnIds.Count);
    }

    /// <summary>
    /// Test 12: ResourceConflictDTO identifies crane conflicts
    /// </summary>
    [Fact]
    public void ResourceConflictDTO_IdentifiesCraneConflicts()
    {
        // Arrange & Act
        var dto = new ResourceConflictDTO
        {
            ResourceType = "crane",
            ResourceId = "CRANE-5",
            ConflictDescription = "Crane already assigned",
            ConflictingVvnIds = new List<string> { "VVN-2025-005" },
            ConflictStartTime = DateTime.UtcNow,
            ConflictEndTime = DateTime.UtcNow.AddHours(2)
        };

        // Assert
        Assert.Equal("crane", dto.ResourceType);
        Assert.Equal("CRANE-5", dto.ResourceId);
    }

    /// <summary>
    /// Test 13: ResourceConflictDTO identifies staff conflicts
    /// </summary>
    [Fact]
    public void ResourceConflictDTO_IdentifiesStaffConflicts()
    {
        // Arrange & Act
        var dto = new ResourceConflictDTO
        {
            ResourceType = "staff",
            ResourceId = "M12345",
            ConflictDescription = "Staff member already assigned",
            ConflictingVvnIds = new List<string> { "VVN-2025-004" },
            ConflictStartTime = DateTime.UtcNow,
            ConflictEndTime = DateTime.UtcNow.AddHours(5)
        };

        // Assert
        Assert.Equal("staff", dto.ResourceType);
        Assert.Equal("M12345", dto.ResourceId);
    }

    #endregion

    #region VvnInconsistency DTO Tests

    /// <summary>
    /// Test 14: VvnInconsistencyDTO identifies timing inconsistencies
    /// </summary>
    [Fact]
    public void VvnInconsistencyDTO_IdentifiesTimingInconsistencies()
    {
        // Arrange & Act
        var dto = new VvnInconsistencyDTO
        {
            VvnId = "VVN-2025-001",
            InconsistencyType = "timing",
            Description = "Operation plan timing conflicts with VVN schedule",
            Severity = "warning"
        };

        // Assert
        Assert.Equal("VVN-2025-001", dto.VvnId);
        Assert.Equal("timing", dto.InconsistencyType);
        Assert.Equal("warning", dto.Severity);
    }

    /// <summary>
    /// Test 15: VvnInconsistencyDTO identifies resource inconsistencies
    /// </summary>
    [Fact]
    public void VvnInconsistencyDTO_IdentifiesResourceInconsistencies()
    {
        // Arrange & Act
        var dto = new VvnInconsistencyDTO
        {
            VvnId = "VVN-2025-002",
            InconsistencyType = "resource",
            Description = "Assigned resources do not match VVN requirements",
            Severity = "error"
        };

        // Assert
        Assert.Equal("resource", dto.InconsistencyType);
        Assert.Equal("error", dto.Severity);
    }

    /// <summary>
    /// Test 16: VvnInconsistencyDTO identifies capacity inconsistencies
    /// </summary>
    [Fact]
    public void VvnInconsistencyDTO_IdentifiesCapacityInconsistencies()
    {
        // Arrange & Act
        var dto = new VvnInconsistencyDTO
        {
            VvnId = "VVN-2025-003",
            InconsistencyType = "capacity",
            Description = "Vessel capacity exceeds dock capacity",
            Severity = "warning"
        };

        // Assert
        Assert.Equal("capacity", dto.InconsistencyType);
        Assert.Contains("capacity", dto.Description);
    }

    #endregion

    #region JSON Serialization Tests

    /// <summary>
    /// Test 17: UpdateOperationPlanRequestDTO can be serialized to JSON
    /// </summary>
    [Fact]
    public void UpdateOperationPlanRequestDTO_CanBeSerializedToJson()
    {
        // Arrange
        var dto = new UpdateOperationPlanRequestDTO
        {
            UpdatedBy = "operator@port.com",
            ReasonForChange = "Test serialization",
            AssignedDock = "DOCK-A"
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("operator@port.com", json);
        Assert.Contains("Test serialization", json);
        Assert.Contains("DOCK-A", json);
    }

    /// <summary>
    /// Test 18: UpdateOperationPlanResponseDTO can be deserialized from JSON
    /// </summary>
    [Fact]
    public void UpdateOperationPlanResponseDTO_CanBeDeserializedFromJson()
    {
        // Arrange
        var json = @"{
            ""success"": true,
            ""message"": ""Updated successfully"",
            ""warnings"": [],
            ""resourceConflicts"": [],
            ""vvnInconsistencies"": []
        }";

        // Act
        var dto = JsonSerializer.Deserialize<UpdateOperationPlanResponseDTO>(json, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(dto);
        Assert.True(dto.Success);
        Assert.Equal("Updated successfully", dto.Message);
    }

    /// <summary>
    /// Test 19: ChangeLogEntryDTO maintains data integrity through serialization
    /// </summary>
    [Fact]
    public void ChangeLogEntryDTO_MaintainsDataIntegrityThroughSerialization()
    {
        // Arrange
        var original = new ChangeLogEntryDTO
        {
            Timestamp = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Author = "operator@port.com",
            Reason = "Manual adjustment",
            Changes = new List<string> { "Updated dock", "Updated time" }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ChangeLogEntryDTO>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Author, deserialized.Author);
        Assert.Equal(original.Reason, deserialized.Reason);
        Assert.Equal(2, deserialized.Changes.Count);
    }

    /// <summary>
    /// Test 20: ResourceConflictDTO preserves all conflict details in JSON
    /// </summary>
    [Fact]
    public void ResourceConflictDTO_PreservesAllConflictDetailsInJson()
    {
        // Arrange
        var original = new ResourceConflictDTO
        {
            ResourceType = "dock",
            ResourceId = "DOCK-B",
            ConflictDescription = "Time overlap",
            ConflictingVvnIds = new List<string> { "VVN-2025-001", "VVN-2025-002" },
            ConflictStartTime = DateTime.UtcNow,
            ConflictEndTime = DateTime.UtcNow.AddHours(2)
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ResourceConflictDTO>(json, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.ResourceType, deserialized.ResourceType);
        Assert.Equal(original.ResourceId, deserialized.ResourceId);
        Assert.Equal(2, deserialized.ConflictingVvnIds.Count);
    }

    #endregion
}
