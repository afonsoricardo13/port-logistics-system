namespace Domain.Model;

public interface IOperatingStaff
{
    string GetMecanographicNumber();
    string GetName();
    string GetEmail();
    string GetPhoneNumber();
    string GetOperationalWindow();
    StaffStatus GetStatus();
    List<string> GetQualifications();
    List<StatusHistoryEntry> GetStatusHistory();
}
