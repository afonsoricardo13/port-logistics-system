namespace Domain.Factory;

using Domain.Model;

public interface IOperatingStaffFactory
{
    OperatingStaff NewOperatingStaff(
        string mecanographicNumber,
        string name,
        string email,
        string phoneNumber,
        string operationalWindow,
        List<string>? qualifications = null);
}
