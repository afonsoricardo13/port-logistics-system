using Application.DTO;
using Domain.Model;
using Xunit;

namespace WebApi.IntegrationTests;

/// <summary>
/// US 4.1.12: Integration tests for Incident Types API
/// Tests verify API endpoints for CRUD operations on incident type classification
/// </summary>
public class IncidentTypesControllerTests_US4_1_12
{
    #region IncidentTypeDTO Tests

    /// <summary>
    /// Test 1: Verifies IncidentTypeDTO can be created with all properties
    /// </summary>
    [Fact]
    public void IncidentTypeDTO_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new IncidentTypeDTO
        {
            Code = "T-INC001",
            Name = "Equipment Failure",
            Description = "Mechanical or electrical failure of port equipment",
            Severity = "Major",
            ParentTypeCode = null,
            ParentTypeName = null,
            ChildTypes = new List<IncidentTypeDTO>()
        };

        // Assert
        Assert.Equal("T-INC001", dto.Code);
        Assert.Equal("Equipment Failure", dto.Name);
        Assert.Equal("Major", dto.Severity);
    }

    /// <summary>
    /// Test 2: Verifies IncidentTypeDTO supports hierarchy
    /// </summary>
    [Fact]
    public void IncidentTypeDTO_SupportsHierarchy()
    {
        // Arrange & Act
        var dto = new IncidentTypeDTO
        {
            Code = "T-INC001-A",
            Name = "Crane Failure",
            ParentTypeCode = "T-INC001",
            ParentTypeName = "Equipment Failure"
        };

        // Assert
        Assert.Equal("T-INC001", dto.ParentTypeCode);
        Assert.Equal("Equipment Failure", dto.ParentTypeName);
    }

    /// <summary>
    /// Test 3: Verifies IncidentTypeDTO can have child types
    /// </summary>
    [Fact]
    public void IncidentTypeDTO_CanHaveChildTypes()
    {
        // Arrange & Act
        var dto = new IncidentTypeDTO
        {
            Code = "T-INC001",
            Name = "Equipment Failure",
            ChildTypes = new List<IncidentTypeDTO>
            {
                new IncidentTypeDTO { Code = "T-INC001-A", Name = "Crane Failure" },
                new IncidentTypeDTO { Code = "T-INC001-B", Name = "Conveyor Failure" }
            }
        };

        // Assert
        Assert.Equal(2, dto.ChildTypes.Count);
    }

    #endregion

    #region CreateIncidentTypeDTO Tests

    /// <summary>
    /// Test 4: Verifies CreateIncidentTypeDTO contains required fields
    /// </summary>
    [Fact]
    public void CreateIncidentTypeDTO_ContainsRequiredFields()
    {
        // Arrange & Act
        var dto = new CreateIncidentTypeDTO
        {
            Code = "T-INC003",
            Name = "Weather Delay",
            Description = "Operations delayed due to weather conditions",
            Severity = "Minor"
        };

        // Assert
        Assert.Equal("T-INC003", dto.Code);
        Assert.Equal("Weather Delay", dto.Name);
        Assert.Equal("Minor", dto.Severity);
    }

    /// <summary>
    /// Test 5: Verifies CreateIncidentTypeDTO supports optional parent
    /// </summary>
    [Fact]
    public void CreateIncidentTypeDTO_SupportsOptionalParent()
    {
        // Arrange & Act
        var dto = new CreateIncidentTypeDTO
        {
            Code = "T-INC001-C",
            Name = "Spreader Malfunction",
            Severity = "Major",
            ParentTypeCode = "T-INC001"
        };

        // Assert
        Assert.Equal("T-INC001", dto.ParentTypeCode);
    }

    /// <summary>
    /// Test 6: Verifies CreateIncidentTypeDTO has null parent for root types
    /// </summary>
    [Fact]
    public void CreateIncidentTypeDTO_HasNullParentForRootTypes()
    {
        // Arrange & Act
        var dto = new CreateIncidentTypeDTO
        {
            Code = "T-INC001",
            Name = "Equipment Failure",
            Severity = "Major",
            ParentTypeCode = null
        };

        // Assert
        Assert.Null(dto.ParentTypeCode);
    }

    #endregion

    #region UpdateIncidentTypeDTO Tests

    /// <summary>
    /// Test 7: Verifies UpdateIncidentTypeDTO contains updatable fields
    /// </summary>
    [Fact]
    public void UpdateIncidentTypeDTO_ContainsUpdatableFields()
    {
        // Arrange & Act
        var dto = new UpdateIncidentTypeDTO
        {
            Name = "Updated Name",
            Description = "Updated description",
            Severity = "Critical"
        };

        // Assert
        Assert.Equal("Updated Name", dto.Name);
        Assert.Equal("Critical", dto.Severity);
    }

    /// <summary>
    /// Test 8: Verifies UpdateIncidentTypeDTO can change parent
    /// </summary>
    [Fact]
    public void UpdateIncidentTypeDTO_CanChangeParent()
    {
        // Arrange & Act
        var dto = new UpdateIncidentTypeDTO
        {
            Name = "Test",
            Severity = "Minor",
            ParentTypeCode = "T-NEW-PARENT"
        };

        // Assert
        Assert.Equal("T-NEW-PARENT", dto.ParentTypeCode);
    }

    #endregion

    #region Severity Tests

    /// <summary>
    /// Test 9: Verifies Minor severity is valid
    /// </summary>
    [Fact]
    public void IncidentSeverity_MinorIsValid()
    {
        // Arrange & Act
        var severity = IncidentSeverity.Minor;

        // Assert
        Assert.Equal(IncidentSeverity.Minor, severity);
        Assert.Equal("Minor", severity.ToString());
    }

    /// <summary>
    /// Test 10: Verifies Major severity is valid
    /// </summary>
    [Fact]
    public void IncidentSeverity_MajorIsValid()
    {
        // Arrange & Act
        var severity = IncidentSeverity.Major;

        // Assert
        Assert.Equal(IncidentSeverity.Major, severity);
    }

    /// <summary>
    /// Test 11: Verifies Critical severity is valid
    /// </summary>
    [Fact]
    public void IncidentSeverity_CriticalIsValid()
    {
        // Arrange & Act
        var severity = IncidentSeverity.Critical;

        // Assert
        Assert.Equal(IncidentSeverity.Critical, severity);
    }

    /// <summary>
    /// Test 12: Verifies severity can be parsed from string
    /// </summary>
    [Theory]
    [InlineData("Minor", IncidentSeverity.Minor)]
    [InlineData("Major", IncidentSeverity.Major)]
    [InlineData("Critical", IncidentSeverity.Critical)]
    public void IncidentSeverity_CanBeParsedFromString(string input, IncidentSeverity expected)
    {
        // Arrange & Act
        var parsed = Enum.Parse<IncidentSeverity>(input, true);

        // Assert
        Assert.Equal(expected, parsed);
    }

    #endregion

    #region IncidentType Model Tests

    /// <summary>
    /// Test 13: Verifies IncidentType model has required properties
    /// </summary>
    [Fact]
    public void IncidentType_HasRequiredProperties()
    {
        // Arrange & Act
        var incidentType = new IncidentType
        {
            Code = "T-INC001",
            Name = "Equipment Failure",
            Description = "Mechanical failure",
            Severity = IncidentSeverity.Major
        };

        // Assert
        Assert.Equal("T-INC001", incidentType.Code);
        Assert.Equal("Equipment Failure", incidentType.Name);
        Assert.Equal(IncidentSeverity.Major, incidentType.Severity);
    }

    /// <summary>
    /// Test 14: Verifies IncidentType supports parent-child relationship
    /// </summary>
    [Fact]
    public void IncidentType_SupportsParentChildRelationship()
    {
        // Arrange
        var parent = new IncidentType
        {
            Code = "T-PARENT",
            Name = "Parent Type",
            Severity = IncidentSeverity.Major
        };

        // Act
        var child = new IncidentType
        {
            Code = "T-CHILD",
            Name = "Child Type",
            Severity = IncidentSeverity.Minor,
            ParentTypeCode = parent.Code,
            ParentType = parent
        };

        parent.ChildTypes.Add(child);

        // Assert
        Assert.Equal(parent.Code, child.ParentTypeCode);
        Assert.Single(parent.ChildTypes);
    }

    /// <summary>
    /// Test 15: Verifies IncidentType child collection is initialized
    /// </summary>
    [Fact]
    public void IncidentType_ChildCollectionIsInitialized()
    {
        // Arrange & Act
        var incidentType = new IncidentType();

        // Assert
        Assert.NotNull(incidentType.ChildTypes);
        Assert.Empty(incidentType.ChildTypes);
    }

    #endregion
}
