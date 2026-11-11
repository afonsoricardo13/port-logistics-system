namespace Domain.Model;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text.RegularExpressions;

public class OperatingStaff : IOperatingStaff
{
    // European phone format: +[country][number] (9-15 digits total)
    private static readonly Regex PhoneNumberRegex = new Regex(@"^\+?[1-9]\d{8,14}$", RegexOptions.Compiled);
    
    [Key]
    private string _mecanographicNumber = string.Empty;
    public string MecanographicNumber
    {
        get { return _mecanographicNumber; }
    }

    private string _strName = string.Empty;
    public string Name
    {
        get { return _strName; }
    }

    private string _strEmail = string.Empty;
    public string Email
    {
        get { return _strEmail; }
    }

    private string _phoneNumber = string.Empty;
    public string PhoneNumber
    {
        get { return _phoneNumber; }
    }

    private string _operationalWindow = string.Empty;
    public string OperationalWindow
    {
        get { return _operationalWindow; }
    }

    // US 2.2.13 integration
    private List<string> _qualifications;
    public List<string> Qualifications
    {
        get { return _qualifications; }
    }

    private StaffStatus _status;
    public StaffStatus Status
    {
        get { return _status; }
    }

    private List<StatusHistoryEntry> _statusHistory;
    public List<StatusHistoryEntry> StatusHistory
    {
        get { return _statusHistory; }
    }

    private OperatingStaff() 
    {
        _qualifications = new List<string>();
        _statusHistory = new List<StatusHistoryEntry>();
    }

    public OperatingStaff(
        string mecanographicNumber,
        string name,
        string email,
        string phoneNumber,
        string operationalWindow,
        List<string>? qualifications = null)
    {
        if (!IsValidParameters(mecanographicNumber, name, email, phoneNumber))
        {
            throw new ArgumentException("Invalid staff parameters");
        }

        if (!string.IsNullOrWhiteSpace(operationalWindow) && !IsValidOperationalWindowFormat(operationalWindow))
        {
            throw new ArgumentException("Start time must be before end time.");
        }

        _mecanographicNumber = mecanographicNumber.Trim().ToUpperInvariant();
        _strName = name.Trim();
        _strEmail = email.Trim().ToLowerInvariant();
        _phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        _operationalWindow = operationalWindow?.Trim() ?? string.Empty;
        _qualifications = qualifications ?? new List<string>();
        _status = StaffStatus.Available;
        _statusHistory = new List<StatusHistoryEntry>();
        
        AddStatusHistory(StaffStatus.Available, "System", "Initial registration");
    }

    private bool IsValidParameters(string mecanographicNumber, string name, string email, string phoneNumber)
    {
        // BR1: mecano must be 3-20 chars, alphanumeric only
        if (string.IsNullOrWhiteSpace(mecanographicNumber))
            throw new ArgumentException("Mecanographic number is required.");

        var trimmedMecano = mecanographicNumber.Trim();
        if (trimmedMecano.Length < 3 || trimmedMecano.Length > 20)
            throw new ArgumentException("Mecanographic number must be between 3 and 20 characters.");

        if (!IsValidMecanographicNumber(trimmedMecano))
            throw new ArgumentException("Mecanographic number must contain only letters and numbers (e.g., OS001, OP-123, STAFF001).");

        // AC1: name 2-100 chars
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");
            
        if (name.Length < 2 || name.Length > 100)
            throw new ArgumentException("Name must be between 2 and 100 characters.");

        if (!IsValidEmailAddress(email))
            throw new ArgumentException("Please enter a valid email address.");

        if (!IsValidPhoneNumber(phoneNumber))
            throw new ArgumentException("Please enter a valid phone number (9-15 digits, e.g., +351912345678).");

        return true;
    }

    private static bool IsValidMecanographicNumber(string mecanographicNumber)
    {
        // Allow alphanumeric characters and hyphens/underscores (common in IDs)
        // Examples: OS001, OP-123, STAFF_001, EMP123
        var regex = new Regex(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
        return regex.IsMatch(mecanographicNumber);
    }

    private bool IsValidEmailAddress(string email)
    {
        var valid = true;
        try
        {
            var emailAddress = new MailAddress(email);
        }
        catch
        {
            valid = false;
        }
        return valid;
    }

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        string cleanedNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        
        if (cleanedNumber.Length < 9 || cleanedNumber.Length > 15)
            return false;
        
        return PhoneNumberRegex.IsMatch(cleanedNumber);
    }

    private static bool IsValidOperationalWindow(string operationalWindow)
    {
        // BR3: weekly schedule, max 2000 chars
        if (string.IsNullOrWhiteSpace(operationalWindow))
            return true;

        return operationalWindow.Length <= 2000;
    }

    private static bool IsValidOperationalWindowFormat(string operationalWindow)
    {
        // Parse format: "Mon-Fri 08:00-16:00" or "Monday-Friday 08:00-16:00"
        var match = Regex.Match(operationalWindow, @"(\d{2}):(\d{2})-(\d{2}):(\d{2})");
        
        if (!match.Success)
            return true; // If format doesn't match, skip time validation

        int startHour = int.Parse(match.Groups[1].Value);
        int startMinute = int.Parse(match.Groups[2].Value);
        int endHour = int.Parse(match.Groups[3].Value);
        int endMinute = int.Parse(match.Groups[4].Value);

        // Convert to minutes for comparison
        int startTimeInMinutes = startHour * 60 + startMinute;
        int endTimeInMinutes = endHour * 60 + endMinute;

        // Start time must be before end time
        return startTimeInMinutes < endTimeInMinutes;
    }
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");
            
        if (name.Length < 2 || name.Length > 100)
            throw new ArgumentException("Name must be between 2 and 100 characters.");

        _strName = name.Trim();
    }

    public void UpdateEmail(string email)
    {
        if (!IsValidEmailAddress(email))
            throw new ArgumentException("Please enter a valid email address.");

        _strEmail = email.Trim().ToLowerInvariant();
    }

    public void UpdatePhoneNumber(string phoneNumber)
    {
        if (IsValidPhoneNumber(phoneNumber))
        {
            _phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        }
        else
        {
            throw new ArgumentException("Invalid phone number format");
        }
    }

    public void UpdateOperationalWindow(string operationalWindow)
    {
        if (!string.IsNullOrWhiteSpace(operationalWindow) && !IsValidOperationalWindowFormat(operationalWindow))
        {
            throw new ArgumentException("Start time must be before end time.");
        }

        if (IsValidOperationalWindow(operationalWindow))
        {
            _operationalWindow = operationalWindow?.Trim() ?? string.Empty;
        }
        else
        {
            throw new ArgumentException("Invalid operational window format");
        }
    }

    public void AddQualification(string qualification)
    {
        // AC5: qualification must exist in system (validated at service layer)
        if (!string.IsNullOrWhiteSpace(qualification) && !_qualifications.Contains(qualification.Trim()))
        {
            _qualifications.Add(qualification.Trim());
        }
    }

    public void RemoveQualification(string qualification)
    {
        _qualifications.Remove(qualification);
    }

    public void Deactivate(string operatorName, string? reason = null)
    {
        // AC3: preserve data for audit - change to Unavailable (not deleted)
        if (_status != StaffStatus.Unavailable)
        {
            SetStatus(StaffStatus.Unavailable, operatorName, reason ?? "Staff member deactivated");
        }
    }

    public void Reactivate(string operatorName, string? reason = null)
    {
        if (_status == StaffStatus.Unavailable)
        {
            SetStatus(StaffStatus.Available, operatorName, reason ?? "Staff member reactivated");
        }
    }

    public void SetAvailable(string operatorName)
    {
        SetStatus(StaffStatus.Available, operatorName, "Status changed to Available");
    }

    public void SetUnavailable(string operatorName, string reason)
    {
        SetStatus(StaffStatus.Unavailable, operatorName, reason);
    }

    private void SetStatus(StaffStatus newStatus, string operatorName, string reason)
    {
        _status = newStatus;
        AddStatusHistory(newStatus, operatorName, reason);
    }

    private void AddStatusHistory(StaffStatus status, string operatorName, string reason)
    {
        _statusHistory.Add(new StatusHistoryEntry
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            OperatorName = operatorName?.Trim() ?? "System",
            Reason = reason?.Trim() ?? "No reason provided"
        });
    }
    public string GetMecanographicNumber() => _mecanographicNumber;
    public string GetName() => _strName;
    public string GetEmail() => _strEmail;
    public string GetPhoneNumber() => _phoneNumber;
    public string GetOperationalWindow() => _operationalWindow;
    public StaffStatus GetStatus() => _status;
    public List<string> GetQualifications() => new List<string>(_qualifications);
    public List<StatusHistoryEntry> GetStatusHistory() => new List<StatusHistoryEntry>(_statusHistory);
}

// Enum for staff status
public enum StaffStatus
{
    Available,
    Unavailable,
    Deactivated
}

// Status history entry for audit trail
public class StatusHistoryEntry
{
    public StaffStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
