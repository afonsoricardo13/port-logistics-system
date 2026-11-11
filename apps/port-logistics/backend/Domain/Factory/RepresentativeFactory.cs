using Domain.Model;

namespace Domain.Factory
{
    public class RepresentativeFactory : IRepresentativeFactory
    {
        public Representative CreateRepresentative(
            string citizenId,
            string name,
            string nationality,
            string email,
            string phoneNumber,
            long organizationId)
        {
            return new Representative(
                citizenId,
                name,
                nationality,
                email,
                phoneNumber,
                organizationId);
        }
    }
}
