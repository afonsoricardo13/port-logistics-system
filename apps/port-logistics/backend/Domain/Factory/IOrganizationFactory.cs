using System;
using Domain.Model;

namespace Domain.Factory
{
    public interface IOrganizationFactory
    {
        Organization CreateOrganization(
            string identifier,
            string legalName,
            string alternativeName,
            string taxNumber,
            Address address,
            Representative initialRepresentative);
    }
}
