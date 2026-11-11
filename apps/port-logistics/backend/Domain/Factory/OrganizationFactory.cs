using Domain.Model;

namespace Domain.Factory
{
    public class OrganizationFactory : IOrganizationFactory
    {
        public Organization CreateOrganization(
            string identifier,
            string legalName,
            string alternativeName,
            string taxNumber,
            Address address,
            Representative initialRepresentative)
        {
            return new Organization(
                identifier,
                legalName,
                alternativeName,
                taxNumber,
                address,
                initialRepresentative);
        }
    }
}
