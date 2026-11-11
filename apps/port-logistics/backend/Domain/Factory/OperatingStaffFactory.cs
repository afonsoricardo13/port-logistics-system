namespace Domain.Factory;

using Domain.Model;

public class OperatingStaffFactory : IOperatingStaffFactory
{
    public OperatingStaff NewOperatingStaff(
        string mecanographicNumber,
        string name,
        string email,
        string phoneNumber,
        string operationalWindow,
        List<string>? qualifications = null)
    {
        return new OperatingStaff(
            mecanographicNumber,
            name,
            email,
            phoneNumber,
            operationalWindow,
            qualifications);
    }
}
