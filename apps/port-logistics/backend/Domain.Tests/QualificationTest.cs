using Domain.Model;
using Xunit;

namespace Domain.Tests;

/// <summary>
/// Unit tests for Qualification domain entity
/// US 2.2.13: Register and manage qualifications (create, update)
/// Tests validate business rules:
/// - BR1: Qualification name must be unique (enforced by being the ID/Key)
/// - BR2: Qualification name must be descriptive (2-100 characters)
/// - BR3: Required number must be non-negative (0 = optional qualification)
/// - BR4: Required number should have reasonable upper limit (max 999 staff)
/// </summary>
public class QualificationTest
{
    #region Constructor Tests - Valid Cases

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateQualification()
    {
        // Arrange
        string qualificationName = "STS Crane Operator";
        int requiredNumber = 5;

        // Act
        var qualification = new Qualification(qualificationName, requiredNumber);

        // Assert
        Assert.NotNull(qualification);
        Assert.Equal("STS CRANE OPERATOR", qualification.QualificationName); // Normalized to UPPER
        Assert.Equal(5, qualification.RequiredNumber);
    }

    [Fact]
    public void Constructor_WithMinimumLengthName_ShouldCreateQualification()
    {
        // Arrange - 2 characters is the minimum
        string qualificationName = "IT";
        int requiredNumber = 3;

        // Act
        var qualification = new Qualification(qualificationName, requiredNumber);

        // Assert
        Assert.NotNull(qualification);
        Assert.Equal("IT", qualification.QualificationName);
        Assert.Equal(3, qualification.RequiredNumber);
    }

    [Fact]
    public void Constructor_WithMaximumLengthName_ShouldCreateQualification()
    {
        // Arrange - 100 characters is the maximum
        string qualificationName = new string('A', 100);
        int requiredNumber = 10;

        // Act
        var qualification = new Qualification(qualificationName, requiredNumber);

        // Assert
        Assert.NotNull(qualification);
        Assert.Equal(100, qualification.QualificationName.Length);
        Assert.Equal(10, qualification.RequiredNumber);
    }

    [Fact]
    public void Constructor_WithZeroRequiredNumber_ShouldCreateQualification()
    {
        // Arrange - 0 means optional qualification
        string qualificationName = "Optional Skill";
        int requiredNumber = 0;

        // Act
        var qualification = new Qualification(qualificationName, requiredNumber);

        // Assert
        Assert.NotNull(qualification);
        Assert.Equal("OPTIONAL SKILL", qualification.QualificationName);
        Assert.Equal(0, qualification.RequiredNumber);
    }

    [Fact]
    public void Constructor_WithMaximumRequiredNumber_ShouldCreateQualification()
    {
        // Arrange - 999 is the maximum reasonable limit
        string qualificationName = "General Worker";
        int requiredNumber = 999;

        // Act
        var qualification = new Qualification(qualificationName, requiredNumber);

        // Assert
        Assert.NotNull(qualification);
        Assert.Equal("GENERAL WORKER", qualification.QualificationName);
        Assert.Equal(999, qualification.RequiredNumber);
    }

    [Fact]
    public void Constructor_WithNameContainingSpaces_ShouldTrimAndNormalize()
    {
        // Arrange
        string qualificationName = "  Truck Driver  ";
        int requiredNumber = 8;

        // Act
        var qualification = new Qualification(qualificationName, requiredNumber);

        // Assert
        Assert.Equal("TRUCK DRIVER", qualification.QualificationName); // Trimmed and uppercase
        Assert.Equal(8, qualification.RequiredNumber);
    }

    [Fact]
    public void Constructor_WithMixedCaseName_ShouldNormalizeToUpperCase()
    {
        // Arrange
        string qualificationName = "YaRd GaNtRy CrAnE OpErAtOr";
        int requiredNumber = 4;

        // Act
        var qualification = new Qualification(qualificationName, requiredNumber);

        // Assert
        Assert.Equal("YARD GANTRY CRANE OPERATOR", qualification.QualificationName);
    }

    #endregion

    #region Constructor Tests - Invalid Qualification Name

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        string? qualificationName = null;
        int requiredNumber = 5;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Qualification(qualificationName!, requiredNumber));
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        string qualificationName = "";
        int requiredNumber = 5;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Qualification(qualificationName, requiredNumber));
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        string qualificationName = "   ";
        int requiredNumber = 5;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Qualification(qualificationName, requiredNumber));
    }

    [Fact]
    public void Constructor_WithSingleCharacterName_ShouldThrowArgumentException()
    {
        // Arrange - Less than 2 characters
        string qualificationName = "A";
        int requiredNumber = 5;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Qualification(qualificationName, requiredNumber));
    }

    [Fact]
    public void Constructor_WithNameExceeding100Characters_ShouldThrowArgumentException()
    {
        // Arrange - 101 characters (exceeds maximum)
        string qualificationName = new string('B', 101);
        int requiredNumber = 5;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Qualification(qualificationName, requiredNumber));
    }

    #endregion

    #region Constructor Tests - Invalid Required Number

    [Fact]
    public void Constructor_WithNegativeRequiredNumber_ShouldThrowArgumentException()
    {
        // Arrange
        string qualificationName = "Crane Operator";
        int requiredNumber = -1;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Qualification(qualificationName, requiredNumber));
    }

    [Fact]
    public void Constructor_WithRequiredNumberExceeding999_ShouldThrowArgumentException()
    {
        // Arrange - 1000 exceeds maximum reasonable limit
        string qualificationName = "Worker";
        int requiredNumber = 1000;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Qualification(qualificationName, requiredNumber));
    }

    #endregion

    #region UpdateRequiredNumber Tests - Valid Cases

    [Fact]
    public void UpdateRequiredNumber_WithValidNumber_ShouldUpdateSuccessfully()
    {
        // Arrange
        var qualification = new Qualification("Crane Operator", 5);

        // Act
        qualification.UpdateRequiredNumber(10);

        // Assert
        Assert.Equal(10, qualification.RequiredNumber);
    }

    [Fact]
    public void UpdateRequiredNumber_ToZero_ShouldUpdateSuccessfully()
    {
        // Arrange - Setting to 0 makes it optional
        var qualification = new Qualification("Safety Officer", 3);

        // Act
        qualification.UpdateRequiredNumber(0);

        // Assert
        Assert.Equal(0, qualification.RequiredNumber);
    }

    [Fact]
    public void UpdateRequiredNumber_ToMaximum_ShouldUpdateSuccessfully()
    {
        // Arrange
        var qualification = new Qualification("General Worker", 50);

        // Act
        qualification.UpdateRequiredNumber(999);

        // Assert
        Assert.Equal(999, qualification.RequiredNumber);
    }

    [Fact]
    public void UpdateRequiredNumber_IncrementValue_ShouldUpdateSuccessfully()
    {
        // Arrange
        var qualification = new Qualification("Truck Driver", 10);

        // Act
        qualification.UpdateRequiredNumber(15);

        // Assert
        Assert.Equal(15, qualification.RequiredNumber);
    }

    [Fact]
    public void UpdateRequiredNumber_DecrementValue_ShouldUpdateSuccessfully()
    {
        // Arrange
        var qualification = new Qualification("Forklift Operator", 20);

        // Act
        qualification.UpdateRequiredNumber(10);

        // Assert
        Assert.Equal(10, qualification.RequiredNumber);
    }

    #endregion

    #region UpdateRequiredNumber Tests - Invalid Cases

    [Fact]
    public void UpdateRequiredNumber_WithNegativeNumber_ShouldThrowArgumentException()
    {
        // Arrange
        var qualification = new Qualification("Crane Operator", 5);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => qualification.UpdateRequiredNumber(-1));
        Assert.Contains("non-negative", exception.Message);
    }

    [Fact]
    public void UpdateRequiredNumber_ExceedingMaximum_ShouldThrowArgumentException()
    {
        // Arrange
        var qualification = new Qualification("Worker", 100);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => qualification.UpdateRequiredNumber(1000));
        Assert.Contains("999", exception.Message);
    }

    #endregion

    #region Equals and GetHashCode Tests

    [Fact]
    public void Equals_WithSameQualificationName_ShouldReturnTrue()
    {
        // Arrange
        var qualification1 = new Qualification("Crane Operator", 5);
        var qualification2 = new Qualification("Crane Operator", 10); // Different requiredNumber

        // Act & Assert
        Assert.True(qualification1.Equals(qualification2));
    }

    [Fact]
    public void Equals_WithSameQualificationNameDifferentCase_ShouldReturnTrue()
    {
        // Arrange - Case-insensitive comparison (both normalized to UPPER)
        var qualification1 = new Qualification("crane operator", 5);
        var qualification2 = new Qualification("CRANE OPERATOR", 5);

        // Act & Assert
        Assert.True(qualification1.Equals(qualification2));
    }

    [Fact]
    public void Equals_WithDifferentQualificationName_ShouldReturnFalse()
    {
        // Arrange
        var qualification1 = new Qualification("Crane Operator", 5);
        var qualification2 = new Qualification("Truck Driver", 5);

        // Act & Assert
        Assert.False(qualification1.Equals(qualification2));
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var qualification = new Qualification("Crane Operator", 5);

        // Act & Assert
        Assert.False(qualification.Equals(null));
    }

    [Fact]
    public void GetHashCode_WithSameQualificationName_ShouldReturnSameHashCode()
    {
        // Arrange
        var qualification1 = new Qualification("Crane Operator", 5);
        var qualification2 = new Qualification("CRANE OPERATOR", 10);

        // Act & Assert
        Assert.Equal(qualification1.GetHashCode(), qualification2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentQualificationName_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var qualification1 = new Qualification("Crane Operator", 5);
        var qualification2 = new Qualification("Truck Driver", 5);

        // Act & Assert
        Assert.NotEqual(qualification1.GetHashCode(), qualification2.GetHashCode());
    }

    #endregion

    #region Getter Methods Tests

    [Fact]
    public void GetQualificationName_ShouldReturnNormalizedName()
    {
        // Arrange
        var qualification = new Qualification("  crane operator  ", 5);

        // Act
        string name = qualification.GetQualificationName();

        // Assert
        Assert.Equal("CRANE OPERATOR", name);
    }

    [Fact]
    public void GetRequiredNumber_ShouldReturnCorrectValue()
    {
        // Arrange
        var qualification = new Qualification("Truck Driver", 15);

        // Act
        int number = qualification.GetRequiredNumber();

        // Assert
        Assert.Equal(15, number);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var qualification = new Qualification("STS Crane Operator", 5);

        // Act
        string result = qualification.ToString();

        // Assert
        Assert.Contains("STS CRANE OPERATOR", result);
        Assert.Contains("5", result);
    }

    [Fact]
    public void ToString_WithZeroRequiredNumber_ShouldShowZero()
    {
        // Arrange
        var qualification = new Qualification("Optional Skill", 0);

        // Act
        string result = qualification.ToString();

        // Assert
        Assert.Contains("OPTIONAL SKILL", result);
        Assert.Contains("0", result);
    }

    #endregion
}
