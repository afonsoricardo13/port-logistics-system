namespace DataModel.Model;

using Domain.Model;
using System.ComponentModel.DataAnnotations;

public class OperatingStaffDataModel
{
    [Key]
    public string MecanographicNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string OperationalWindow { get; set; } = string.Empty;
    
    // Stored as comma-separated string in database
    public string Qualifications { get; set; } = string.Empty;
    
    public int Status { get; set; }
    
    // JSON serialized status history
    public string StatusHistory { get; set; } = string.Empty;

    public OperatingStaffDataModel() { }

    public OperatingStaffDataModel(OperatingStaff staff)
    {
        MecanographicNumber = staff.MecanographicNumber;
        Name = staff.Name;
        Email = staff.Email;
        PhoneNumber = staff.PhoneNumber;
        OperationalWindow = staff.OperationalWindow;
        
        // Convert qualifications list to comma-separated string
        Qualifications = staff.Qualifications != null && staff.Qualifications.Any()
            ? string.Join(",", staff.Qualifications)
            : string.Empty;
        
        Status = (int)staff.Status;
        
        // Serialize status history to JSON
        StatusHistory = System.Text.Json.JsonSerializer.Serialize(staff.StatusHistory);
    }
}
