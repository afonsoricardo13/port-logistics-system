using Domain.Model;
using Xunit;

namespace Domain.Tests;

/// <summary>
/// Unit tests for OperatingStaff domain entity
/// US 2.2.11: Register and manage operating staff members (create, update, deactivate)
/// Tests validate domain business rules and entity behavior
/// </summary>
public class OperatingStaffTests
{
    #region Constructor Tests - Valid Cases

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var staff = new OperatingStaff(
            "EMP001",
            "John Doe",
            "john.doe@port.com",
            "+351912345678",
            "Mon-Fri 08:00-17:00",
            new List<string> { "Crane Operator", "Safety Officer" }
        );

        // Assert
        Assert.Equal("EMP001", staff.MecanographicNumber);
        Assert.Equal("John Doe", staff.Name);
        Assert.Equal("john.doe@port.com", staff.Email);
        Assert.Equal("+351912345678", staff.PhoneNumber);
        Assert.Equal("Mon-Fri 08:00-17:00", staff.OperationalWindow);
        Assert.Equal(2, staff.Qualifications.Count);
        Assert.Equal(StaffStatus.Available, staff.Status);
        Assert.Single(staff.StatusHistory); // Initial registration entry
    }

    [Fact]
    public void Create_WithMinimumValidData_ShouldSucceed()
    {
        // Arrange & Act - Minimum: 3 chars mecano, 2 chars name, 9 digit phone
        var staff = new OperatingStaff(
            "ABC",
            "Jo",
            "a@b.c",
            "+123456789",
            "Mon-Fri 08:00-17:00",
            null
        );

        // Assert
        Assert.Equal("ABC", staff.MecanographicNumber);
        Assert.Equal("Jo", staff.Name);
        Assert.Empty(staff.Qualifications);
        Assert.Equal(StaffStatus.Available, staff.Status);
    }

    [Fact]
    public void Create_WithMaximumValidData_ShouldSucceed()
    {
        // Arrange - Max: 20 chars mecano, 100 chars name, 15 digit phone, 2000 chars window
        var longMecano = new string('A', 20);
        var longName = new string('B', 100);
        var longWindow = new string('C', 2000);

        // Act
        var staff = new OperatingStaff(
            longMecano,
            longName,
            "test@example.com",
            "+12345678901234",
            longWindow,
            new List<string> { "Q1", "Q2", "Q3" }
        );

        // Assert
        Assert.Equal(20, staff.MecanographicNumber.Length);
        Assert.Equal(100, staff.Name.Length);
        Assert.Equal(2000, staff.OperationalWindow.Length);
        Assert.Equal(3, staff.Qualifications.Count);
    }

    [Fact]
    public void Create_WithWhitespaceInFields_ShouldTrimAndNormalize()
    {
        // Arrange & Act
        var staff = new OperatingStaff(
            "  emp001  ",
            "  John Doe  ",
            "  JOHN@PORT.COM  ",
            "+351 912 345 678",
            "  Mon-Fri  ",
            null
        );

        // Assert
        Assert.Equal("EMP001", staff.MecanographicNumber); // Trimmed and uppercase
        Assert.Equal("John Doe", staff.Name); // Trimmed
        Assert.Equal("john@port.com", staff.Email); // Trimmed and lowercase
        Assert.Equal("+351912345678", staff.PhoneNumber); // Spaces removed
    }

    [Fact]
    public void Create_WithPhoneNumberFormatting_ShouldRemoveFormatting()
    {
        // Arrange & Act
        var staff = new OperatingStaff(
            "EMP001",
            "John Doe",
            "john@port.com",
            "+351 (912) 345-678",
            "Mon-Fri 08:00-17:00",
            null
        );

        // Assert
        Assert.Equal("+351912345678", staff.PhoneNumber);
    }

    #endregion

    #region Constructor Tests - Invalid Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidMecanographicNumber_ShouldThrow(string? mecano)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OperatingStaff(
            mecano!,
            "John Doe",
            "john@port.com",
            "+351912345678",
            "",
            null
        ));
    }

    [Theory]
    [InlineData("AB")] // Too short (< 3)
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")] // Too long (> 20)
    [InlineData("EMP 001")] // Contains space
    [InlineData("EMP@001")] // Contains special char
    [InlineData("EMP.001")] // Contains dot
    public void Create_WithMecanographicNumberOutOfRange_ShouldThrow(string mecano)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OperatingStaff(
            mecano,
            "John Doe",
            "john@port.com",
            "+351912345678",
            "",
            null
        ));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")] // Too short (< 2)
    public void Create_WithInvalidName_ShouldThrow(string? name)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OperatingStaff(
            "EMP001",
            name!,
            "john@port.com",
            "+351912345678",
            "",
            null
        ));
    }

    [Fact]
    public void Create_WithNameExceeding100Characters_ShouldThrow()
    {
        // Arrange
        var longName = new string('A', 101);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OperatingStaff(
            "EMP001",
            longName,
            "john@port.com",
            "+351912345678",
            "",
            null
        ));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Create_WithInvalidEmail_ShouldThrow(string? email)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OperatingStaff(
            "EMP001",
            "John Doe",
            email!,
            "+351912345678",
            "",
            null
        ));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678")] // Too short (< 9)
    [InlineData("+12345678901234567")] // Too long (> 16)
    [InlineData("abcdefghijk")] // Non-numeric
    [InlineData("0123456789")] // Starts with 0
    public void Create_WithInvalidPhoneNumber_ShouldThrow(string? phone)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OperatingStaff(
            "EMP001",
            "John Doe",
            "john@port.com",
            phone!,
            "",
            null
        ));
    }

    #endregion

    #region Update Methods Tests

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdate()
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act
        staff.UpdateName("Jane Smith");

        // Assert
        Assert.Equal("Jane Smith", staff.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")] // Too short
    public void UpdateName_WithInvalidName_ShouldThrow(string? name)
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => staff.UpdateName(name!));
    }

    [Fact]
    public void UpdateEmail_WithValidEmail_ShouldUpdate()
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act
        staff.UpdateEmail("new.email@port.com");

        // Assert
        Assert.Equal("new.email@port.com", staff.Email);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    public void UpdateEmail_WithInvalidEmail_ShouldThrow(string email)
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => staff.UpdateEmail(email));
    }

    [Fact]
    public void UpdatePhoneNumber_WithValidPhone_ShouldUpdate()
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act
        staff.UpdatePhoneNumber("+351987654321");

        // Assert
        Assert.Equal("+351987654321", staff.PhoneNumber);
    }

    [Theory]
    [InlineData("12345678")] // Too short
    [InlineData("abcdefghijk")] // Non-numeric
    public void UpdatePhoneNumber_WithInvalidPhone_ShouldThrow(string phone)
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => staff.UpdatePhoneNumber(phone));
    }

    [Fact]
    public void UpdateOperationalWindow_WithValidWindow_ShouldUpdate()
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act
        staff.UpdateOperationalWindow("Mon-Sun 00:00-23:59");

        // Assert
        Assert.Equal("Mon-Sun 00:00-23:59", staff.OperationalWindow);
    }

    [Fact]
    public void UpdateOperationalWindow_WithEmptyString_ShouldUpdate()
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act
        staff.UpdateOperationalWindow("");

        // Assert
        Assert.Equal("", staff.OperationalWindow);
    }

    [Fact]
    public void UpdateOperationalWindow_WithExcessiveLength_ShouldThrow()
    {
        // Arrange
        var staff = CreateValidStaff();
        var longWindow = new string('A', 2001);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => staff.UpdateOperationalWindow(longWindow));
    }

    [Theory]
    [InlineData("Mon-Fri 18:00-09:00")] // Start after end
    [InlineData("Mon-Fri 23:59-00:00")] // Start after end
    public void Create_WithInvalidOperationalWindowTime_ShouldThrow(string window)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OperatingStaff(
            "EMP001",
            "John Doe",
            "john@port.com",
            "+351912345678",
            window,
            null
        ));
    }

    [Theory]
    [InlineData("Mon-Fri 18:00-09:00")] // Start after end
    [InlineData("Mon-Fri 23:59-00:00")] // Start after end
    public void UpdateOperationalWindow_WithInvalidTime_ShouldThrow(string window)
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => staff.UpdateOperationalWindow(window));
    }

    #endregion

    #region Qualification Management Tests

    [Fact]
    public void AddQualification_WithValidQualification_ShouldAdd()
    {
        // Arrange
        var staff = CreateValidStaff();
        var initialCount = staff.Qualifications.Count;

        // Act
        staff.AddQualification("Forklift Operator");

        // Assert
        Assert.Equal(initialCount + 1, staff.Qualifications.Count);
        Assert.Contains("Forklift Operator", staff.Qualifications);
    }

    [Fact]
    public void AddQualification_WithDuplicateQualification_ShouldNotAdd()
    {
        // Arrange
        var staff = CreateValidStaff();
        staff.AddQualification("Safety Officer");
        var countBefore = staff.Qualifications.Count;

        // Act
        staff.AddQualification("Safety Officer");

        // Assert
        Assert.Equal(countBefore, staff.Qualifications.Count); // No duplicate added
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddQualification_WithInvalidQualification_ShouldNotAdd(string? qualification)
    {
        // Arrange
        var staff = CreateValidStaff();
        var countBefore = staff.Qualifications.Count;

        // Act
        staff.AddQualification(qualification!);

        // Assert
        Assert.Equal(countBefore, staff.Qualifications.Count);
    }

    [Fact]
    public void RemoveQualification_WithExistingQualification_ShouldRemove()
    {
        // Arrange
        var staff = CreateValidStaff();
        staff.AddQualification("Temporary Qual");
        Assert.Contains("Temporary Qual", staff.Qualifications);

        // Act
        staff.RemoveQualification("Temporary Qual");

        // Assert
        Assert.DoesNotContain("Temporary Qual", staff.Qualifications);
    }

    [Fact]
    public void RemoveQualification_WithNonExistingQualification_ShouldNotThrow()
    {
        // Arrange
        var staff = CreateValidStaff();
        var countBefore = staff.Qualifications.Count;

        // Act
        staff.RemoveQualification("NonExistent");

        // Assert
        Assert.Equal(countBefore, staff.Qualifications.Count);
    }

    #endregion

    #region Status Management Tests

    [Fact]
    public void Deactivate_WhenAvailable_ShouldChangeStatusToUnavailable()
    {
        // Arrange
        var staff = CreateValidStaff();
        Assert.Equal(StaffStatus.Available, staff.Status);

        // Act
        staff.Deactivate("Admin", "End of contract");

        // Assert
        Assert.Equal(StaffStatus.Unavailable, staff.Status);
        Assert.True(staff.StatusHistory.Count >= 2); // Initial + deactivation
    }

    [Fact]
    public void Deactivate_WhenAlreadyUnavailable_ShouldNotAddDuplicateHistory()
    {
        // Arrange
        var staff = CreateValidStaff();
        staff.Deactivate("Admin", "First deactivation");
        var historyCountAfterFirst = staff.StatusHistory.Count;

        // Act
        staff.Deactivate("Admin", "Second deactivation");

        // Assert
        Assert.Equal(StaffStatus.Unavailable, staff.Status);
        Assert.Equal(historyCountAfterFirst, staff.StatusHistory.Count); // No new entry
    }

    [Fact]
    public void Reactivate_WhenUnavailable_ShouldChangeStatusToAvailable()
    {
        // Arrange
        var staff = CreateValidStaff();
        staff.Deactivate("Admin", "Temporary leave");

        // Act
        staff.Reactivate("Admin", "Returned from leave");

        // Assert
        Assert.Equal(StaffStatus.Available, staff.Status);
        Assert.True(staff.StatusHistory.Count >= 3); // Initial + deactivate + reactivate
    }

    [Fact]
    public void Reactivate_WhenAlreadyAvailable_ShouldNotAddDuplicateHistory()
    {
        // Arrange
        var staff = CreateValidStaff();
        var historyCountInitial = staff.StatusHistory.Count;

        // Act
        staff.Reactivate("Admin", "Already active");

        // Assert
        Assert.Equal(StaffStatus.Available, staff.Status);
        Assert.Equal(historyCountInitial, staff.StatusHistory.Count); // No new entry
    }

    [Fact]
    public void SetAvailable_ShouldChangeStatusAndAddHistory()
    {
        // Arrange
        var staff = CreateValidStaff();
        staff.Deactivate("Admin", "Test");

        // Act
        staff.SetAvailable("Supervisor");

        // Assert
        Assert.Equal(StaffStatus.Available, staff.Status);
        var lastEntry = staff.StatusHistory.Last();
        Assert.Equal("Supervisor", lastEntry.OperatorName);
    }

    [Fact]
    public void SetUnavailable_ShouldChangeStatusAndAddHistory()
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act
        staff.SetUnavailable("Manager", "Medical leave");

        // Assert
        Assert.Equal(StaffStatus.Unavailable, staff.Status);
        var lastEntry = staff.StatusHistory.Last();
        Assert.Equal("Manager", lastEntry.OperatorName);
        Assert.Equal("Medical leave", lastEntry.Reason);
    }

    [Fact]
    public void StatusHistory_ShouldContainInitialEntry()
    {
        // Arrange & Act
        var staff = CreateValidStaff();

        // Assert
        Assert.Single(staff.StatusHistory);
        var initialEntry = staff.StatusHistory.First();
        Assert.Equal(StaffStatus.Available, initialEntry.Status);
        Assert.Equal("System", initialEntry.OperatorName);
        Assert.Equal("Initial registration", initialEntry.Reason);
    }

    [Fact]
    public void StatusHistory_ShouldPreserveAuditTrail()
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act
        staff.Deactivate("Admin1", "Reason1");
        staff.Reactivate("Admin2", "Reason2");
        staff.SetUnavailable("Admin3", "Reason3");

        // Assert
        Assert.Equal(4, staff.StatusHistory.Count); // Initial + 3 changes
        Assert.Equal("Admin1", staff.StatusHistory[1].OperatorName);
        Assert.Equal("Admin2", staff.StatusHistory[2].OperatorName);
        Assert.Equal("Admin3", staff.StatusHistory[3].OperatorName);
    }

    #endregion

    #region Getter Methods Tests

    [Fact]
    public void GetMethods_ShouldReturnCorrectValues()
    {
        // Arrange
        var staff = CreateValidStaff();

        // Act & Assert
        Assert.Equal(staff.MecanographicNumber, staff.GetMecanographicNumber());
        Assert.Equal(staff.Name, staff.GetName());
        Assert.Equal(staff.Email, staff.GetEmail());
        Assert.Equal(staff.PhoneNumber, staff.GetPhoneNumber());
        Assert.Equal(staff.OperationalWindow, staff.GetOperationalWindow());
        Assert.Equal(staff.Status, staff.GetStatus());
    }

    [Fact]
    public void GetQualifications_ShouldReturnCopy()
    {
        // Arrange
        var staff = CreateValidStaff();
        
        // Act
        var qualifications = staff.GetQualifications();
        qualifications.Add("New Qualification");

        // Assert
        Assert.DoesNotContain("New Qualification", staff.Qualifications); // Original unchanged
    }

    [Fact]
    public void GetStatusHistory_ShouldReturnCopy()
    {
        // Arrange
        var staff = CreateValidStaff();
        
        // Act
        var history = staff.GetStatusHistory();
        history.Clear();

        // Assert
        Assert.NotEmpty(staff.StatusHistory); // Original unchanged
    }

    #endregion

    #region Helper Methods

    private OperatingStaff CreateValidStaff()
    {
        return new OperatingStaff(
            "EMP001",
            "John Doe",
            "john.doe@port.com",
            "+351912345678",
            "Mon-Fri 08:00-17:00",
            new List<string> { "Crane Operator" }
        );
    }

    #endregion
}
