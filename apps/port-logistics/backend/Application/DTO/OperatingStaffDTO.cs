namespace Application.DTO;

using Domain.Model;

// DTO for creating new Operating Staff (without Status - set automatically)
public class CreateOperatingStaffDTO
{
    public string MecanographicNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string OperationalWindow { get; set; } = string.Empty;
    public List<string> Qualifications { get; set; }

    public CreateOperatingStaffDTO()
    {
        Qualifications = new List<string>();
    }
}

// DTO for updating Operating Staff (without Status and MecanographicNumber)
public class UpdateOperatingStaffDTO
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string OperationalWindow { get; set; } = string.Empty;
    public List<string> Qualifications { get; set; }

    public UpdateOperatingStaffDTO()
    {
        Qualifications = new List<string>();
    }
}

public class OperatingStaffDTO
{
    // Don't expose persistence ID
    public string MecanographicNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string OperationalWindow { get; set; } = string.Empty;
    public List<string> Qualifications { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<StatusHistoryDTO> StatusHistory { get; set; }

    public OperatingStaffDTO()
    {
        Qualifications = new List<string>();
        StatusHistory = new List<StatusHistoryDTO>();
    }

    public OperatingStaffDTO(
        string mecanographicNumber,
        string name,
        string email,
        string phoneNumber,
        string operationalWindow,
        List<string> qualifications,
        string status,
        List<StatusHistoryDTO> statusHistory)
    {
        MecanographicNumber = mecanographicNumber;
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        OperationalWindow = operationalWindow;
        Qualifications = qualifications ?? new List<string>();
        Status = status;
        StatusHistory = statusHistory ?? new List<StatusHistoryDTO>();
    }

    // Convert domain to DTO
    public static OperatingStaffDTO ToDTO(OperatingStaff staff)
    {
        return new OperatingStaffDTO(
            staff.MecanographicNumber,
            staff.Name,
            staff.Email,
            staff.PhoneNumber,
            staff.OperationalWindow,
            new List<string>(staff.Qualifications),
            staff.Status.ToString(),
            staff.StatusHistory.Select(StatusHistoryDTO.ToDTO).ToList()
        );
    }

    public static IEnumerable<OperatingStaffDTO> ToDTO(IEnumerable<OperatingStaff> staffList)
    {
        return staffList.Select(ToDTO).ToList();
    }

    // Convert DTO to domain (for creation)
    public static OperatingStaff ToDomain(CreateOperatingStaffDTO dto)
    {
        return new OperatingStaff(
            dto.MecanographicNumber,
            dto.Name,
            dto.Email,
            dto.PhoneNumber,
            dto.OperationalWindow,
            dto.Qualifications
        );
    }

    // Update domain object from DTO
    public static OperatingStaff UpdateToDomain(OperatingStaff staff, UpdateOperatingStaffDTO dto)
    {
        // Don't allow changing mecanographic number (unique identifier)
        
        staff.UpdateName(dto.Name);
        staff.UpdateEmail(dto.Email);
        staff.UpdatePhoneNumber(dto.PhoneNumber);
        staff.UpdateOperationalWindow(dto.OperationalWindow);

        // Update qualifications
        var currentQualifications = staff.GetQualifications();
        var newQualifications = dto.Qualifications;

        // Remove qualifications not in the new list
        foreach (var qual in currentQualifications)
        {
            if (!newQualifications.Contains(qual))
            {
                staff.RemoveQualification(qual);
            }
        }

        // Add new qualifications
        foreach (var qual in newQualifications)
        {
            staff.AddQualification(qual);
        }

        return staff;
    }
}

public class StatusHistoryDTO
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public StatusHistoryDTO() { }

    public StatusHistoryDTO(string status, DateTime timestamp, string operatorName, string reason)
    {
        Status = status;
        Timestamp = timestamp;
        OperatorName = operatorName;
        Reason = reason;
    }

    public static StatusHistoryDTO ToDTO(StatusHistoryEntry entry)
    {
        return new StatusHistoryDTO(
            entry.Status.ToString(),
            entry.Timestamp,
            entry.OperatorName,
            entry.Reason
        );
    }
}
