using System.Collections.Generic;
using System.Linq;
using Domain.Model;

namespace Application.DTO
{
    public class AddressDTO
    {
        public string Street { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public Address ToDomain()
        {
            return new Address(Street, PostalCode, City);
        }

        public static AddressDTO FromDomain(Address address)
        {
            return new AddressDTO
            {
                Street = address.Street,
                PostalCode = address.PostalCode,
                City = address.City
            };
        }
    }

    public class OrganizationDTO
    {
        public long Id { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string AlternativeName { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public AddressDTO Address { get; set; } = new();
        public List<RepresentativeDTO> Representatives { get; set; } = new();

        public static OrganizationDTO FromDomain(Organization organization)
        {
            return new OrganizationDTO
            {
                Id = organization.Id,
                Identifier = organization.Identifier,
                LegalName = organization.LegalName,
                AlternativeName = organization.AlternativeName,
                TaxNumber = organization.TaxNumber,
                Address = AddressDTO.FromDomain(organization.Address),
                Representatives = organization.Representatives
                    .Select(RepresentativeDTO.FromDomain)
                    .ToList()
            };
        }
    }

    public class CreateOrganizationDTO
    {
        public string Identifier { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string AlternativeName { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public AddressDTO Address { get; set; } = new();
        public CreateRepresentativeDTO InitialRepresentative { get; set; } = new();
    }

    public class UpdateOrganizationDTO
    {
        public string LegalName { get; set; } = string.Empty;
        public string AlternativeName { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public AddressDTO Address { get; set; } = new();
    }
}
