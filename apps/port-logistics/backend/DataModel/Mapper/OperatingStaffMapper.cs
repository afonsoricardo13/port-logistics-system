namespace DataModel.Mapper;

using DataModel.Model;
using Domain.Model;
using System.Text.Json;

public class OperatingStaffMapper
{
    public static OperatingStaff? ToDomain(OperatingStaffDataModel dataModel)
    {
        if (dataModel == null)
            return null;

        // Parse qualifications from comma-separated string
        List<string> qualifications = !string.IsNullOrWhiteSpace(dataModel.Qualifications)
            ? dataModel.Qualifications.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            : new List<string>();

        // Create domain object
        var staff = new OperatingStaff(
            dataModel.MecanographicNumber,
            dataModel.Name,
            dataModel.Email,
            dataModel.PhoneNumber,
            dataModel.OperationalWindow,
            qualifications
        );

        // Restore status and status history using reflection (since constructors set initial status)
        var statusField = typeof(OperatingStaff).GetField("_status", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        statusField?.SetValue(staff, (StaffStatus)dataModel.Status);

        if (!string.IsNullOrWhiteSpace(dataModel.StatusHistory))
        {
            try
            {
                // Try to parse as JSON (new format)
                var statusHistory = JsonSerializer.Deserialize<List<StatusHistoryEntry>>(dataModel.StatusHistory);
                var historyField = typeof(OperatingStaff).GetField("_statusHistory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                historyField?.SetValue(staff, statusHistory);
            }
            catch (JsonException)
            {
                // Legacy format (plain text like "Active since 2025-10-05") - ignore or create empty history
                // Will be replaced when status is updated via domain methods
                var historyField = typeof(OperatingStaff).GetField("_statusHistory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                historyField?.SetValue(staff, new List<StatusHistoryEntry>());
            }
        }

        return staff;
    }

    public static OperatingStaffDataModel? ToDataModel(OperatingStaff domain)
    {
        if (domain == null)
            return null;

        return new OperatingStaffDataModel(domain);
    }

    public static List<OperatingStaff> ToDomainList(List<OperatingStaffDataModel> dataModels)
    {
        if (dataModels == null) return new List<OperatingStaff>();
        var result = new List<OperatingStaff>();
        foreach (var dm in dataModels)
        {
            var domain = ToDomain(dm);
            if (domain != null) result.Add(domain);
        }
        return result;
    }

    public static List<OperatingStaffDataModel> ToDataModelList(List<OperatingStaff> domains)
    {
        if (domains == null) return new List<OperatingStaffDataModel>();
        var result = new List<OperatingStaffDataModel>();
        foreach (var d in domains)
        {
            var dataModel = ToDataModel(d);
            if (dataModel != null) result.Add(dataModel);
        }
        return result;
    }
}
