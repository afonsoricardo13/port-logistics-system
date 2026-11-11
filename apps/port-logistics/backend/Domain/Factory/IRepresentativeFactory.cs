using Domain.Model;

namespace Domain.Factory
{
    public interface IRepresentativeFactory
    {
        Representative CreateRepresentative(
            string citizenId,
            string name,
            string nationality,
            string email,
            string phoneNumber,
            long organizationId);
    }
}
