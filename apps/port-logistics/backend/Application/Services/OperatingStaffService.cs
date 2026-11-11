namespace Application.Services;

using Domain.Model;
using Application.DTO;
using Domain.IRepository;

public class OperatingStaffService
{
    private readonly IOperatingStaffRepository _staffRepository;
    private readonly QualificationService _qualificationService;

    public OperatingStaffService(
        IOperatingStaffRepository staffRepository,
        QualificationService qualificationService)
    {
        _staffRepository = staffRepository;
        _qualificationService = qualificationService;
    }

    // Get all staff
    public async Task<IEnumerable<OperatingStaffDTO>> GetAllAsync()
    {
        List<OperatingStaff> staffList = await _staffRepository.GetAllAsync();
        return OperatingStaffDTO.ToDTO(staffList);
    }

    // Get staff by mecanographic number
    public async Task<OperatingStaffDTO?> GetByMecanographicNumberAsync(string mecanographicNumber)
    {
        OperatingStaff? staff = await _staffRepository.GetByMecanographicNumberAsync(mecanographicNumber);
        
        if (staff != null)
        {
            return OperatingStaffDTO.ToDTO(staff);
        }
        return null;
    }

    // Get staff by status
    public async Task<IEnumerable<OperatingStaffDTO>> GetByStatusAsync(string status)
    {
        if (Enum.TryParse<StaffStatus>(status, true, out StaffStatus staffStatus))
        {
            List<OperatingStaff> staffList = await _staffRepository.GetByStatusAsync(staffStatus);
            return OperatingStaffDTO.ToDTO(staffList);
        }
        return new List<OperatingStaffDTO>();
    }

    // Search staff with filters
    public async Task<IEnumerable<OperatingStaffDTO>> SearchAsync(string? name = null, string? email = null, string? status = null, string? qualification = null)
    {
        StaffStatus? staffStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<StaffStatus>(status, true, out StaffStatus parsedStatus))
            {
                staffStatus = parsedStatus;
            }
        }

        List<OperatingStaff> staffList = await _staffRepository.SearchAsync(name, email, staffStatus, qualification);
        return OperatingStaffDTO.ToDTO(staffList);
    }

    public async Task<OperatingStaffDTO?> AddAsync(CreateOperatingStaffDTO staffDTO, List<string> errorMessages)
    {
        bool exists = await _staffRepository.ExistsByMecanographicNumberAsync(staffDTO.MecanographicNumber);
        if (exists)
        {
            errorMessages.Add("Staff member with this mecanographic number already exists");
            return null;
        }

        // AC5: validate qualifications exist in system
        if (staffDTO.Qualifications != null && staffDTO.Qualifications.Any())
        {
            foreach (var qualificationName in staffDTO.Qualifications)
            {
                bool qualExists = await _qualificationService.ExistsByQualificationNameAsync(qualificationName);
                if (!qualExists)
                {
                    errorMessages.Add($"Qualification '{qualificationName}' does not exist. Register it first (US 2.2.13)");
                    return null;
                }
            }
        }

        try
        {
            OperatingStaff staff = OperatingStaffDTO.ToDomain(staffDTO);
            OperatingStaff savedStaff = await _staffRepository.AddAsync(staff);
            return OperatingStaffDTO.ToDTO(savedStaff);
        }
        catch (ArgumentException ex)
        {
            // Domain validation failed - return specific error message
            errorMessages.Add(ex.Message);
            return null;
        }
    }

    public async Task<OperatingStaffDTO?> UpdateAsync(string mecanographicNumber, UpdateOperatingStaffDTO staffDTO, List<string> errorMessages)
    {
        OperatingStaff? staff = await _staffRepository.GetByMecanographicNumberAsync(mecanographicNumber);

        if (staff == null)
        {
            errorMessages.Add("Staff member not found");
            return null;
        }

        if (staffDTO.Qualifications != null && staffDTO.Qualifications.Any())
        {
            foreach (var qualificationName in staffDTO.Qualifications)
            {
                bool qualExists = await _qualificationService.ExistsByQualificationNameAsync(qualificationName);
                if (!qualExists)
                {
                    errorMessages.Add($"Qualification '{qualificationName}' does not exist. Register it first (US 2.2.13)");
                    return null;
                }
            }
        }

        try
        {
            OperatingStaffDTO.UpdateToDomain(staff, staffDTO);
            OperatingStaff? updatedStaff = await _staffRepository.UpdateAsync(staff);

            if (updatedStaff == null)
            {
                errorMessages.Add("Failed to update staff member");
                return null;
            }

            return OperatingStaffDTO.ToDTO(updatedStaff);
        }
        catch (ArgumentException ex)
        {
            // Domain validation failed - return specific error message
            errorMessages.Add(ex.Message);
            return null;
        }
    }

    // Deactivate staff (sets to Unavailable)
    public async Task<OperatingStaffDTO?> DeactivateAsync(string mecanographicNumber, string operatorName, string? reason, List<string> errorMessages)
    {
        OperatingStaff? staff = await _staffRepository.GetByMecanographicNumberAsync(mecanographicNumber);

        if (staff == null)
        {
            errorMessages.Add("Staff member not found");
            return null;
        }

        if (staff.Status == StaffStatus.Unavailable)
        {
            errorMessages.Add("Staff member is already unavailable");
            return null;
        }

        staff.Deactivate(operatorName, reason);
        OperatingStaff? updatedStaff = await _staffRepository.UpdateAsync(staff);

        if (updatedStaff == null)
        {
            errorMessages.Add("Failed to deactivate staff member");
            return null;
        }

        return OperatingStaffDTO.ToDTO(updatedStaff);
    }

    // Reactivate staff (sets to Available)
    public async Task<OperatingStaffDTO?> ReactivateAsync(string mecanographicNumber, string operatorName, string? reason, List<string> errorMessages)
    {
        OperatingStaff? staff = await _staffRepository.GetByMecanographicNumberAsync(mecanographicNumber);

        if (staff == null)
        {
            errorMessages.Add("Staff member not found");
            return null;
        }

        if (staff.Status != StaffStatus.Unavailable)
        {
            errorMessages.Add("Staff member is not unavailable");
            return null;
        }

        staff.Reactivate(operatorName, reason);
        OperatingStaff? updatedStaff = await _staffRepository.UpdateAsync(staff);

        if (updatedStaff == null)
        {
            errorMessages.Add("Failed to reactivate staff member");
            return null;
        }

        return OperatingStaffDTO.ToDTO(updatedStaff);
    }
}
