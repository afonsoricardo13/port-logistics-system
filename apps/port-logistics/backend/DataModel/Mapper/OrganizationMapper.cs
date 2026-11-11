using System.Linq;
using DataModel.Model;
using Domain.Model;

namespace DataModel.Mapper
{
    public class OrganizationMapper
    {
        private readonly RepresentativeMapper _representativeMapper;

        public OrganizationMapper(RepresentativeMapper representativeMapper)
        {
            _representativeMapper = representativeMapper;
        }

        public OrganizationDataModel ToDataModel(Organization domain)
        {
            return new OrganizationDataModel
            {
                Id = domain.Id,
                Identifier = domain.Identifier,
                LegalName = domain.LegalName,
                AlternativeName = domain.AlternativeName,
                TaxNumber = domain.TaxNumber,
                Street = domain.Address.Street,
                PostalCode = domain.Address.PostalCode,
                City = domain.Address.City,
                Representatives = domain.Representatives
                    .Select(_representativeMapper.ToDataModel)
                    .ToList()
            };
        }

        public Organization ToDomain(OrganizationDataModel dataModel)
        {
            var address = new Address(
                dataModel.Street,
                dataModel.PostalCode,
                dataModel.City
            );

            // We need to use reflection to set the Id since Organization constructor doesn't accept it
            var organization = (Organization)System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(typeof(Organization));

            typeof(Organization).GetProperty("Id")!
                .SetValue(organization, dataModel.Id);
            typeof(Organization).GetProperty("Identifier")!
                .SetValue(organization, dataModel.Identifier);
            typeof(Organization).GetProperty("LegalName")!
                .SetValue(organization, dataModel.LegalName);
            typeof(Organization).GetProperty("AlternativeName")!
                .SetValue(organization, dataModel.AlternativeName);
            typeof(Organization).GetProperty("TaxNumber")!
                .SetValue(organization, dataModel.TaxNumber);
            typeof(Organization).GetProperty("Address")!
                .SetValue(organization, address);

            // Load representatives
            var representativesField = typeof(Organization)
                .GetField("_representatives", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var representativesList = dataModel.Representatives
                .Select(_representativeMapper.ToDomain)
                .ToList();
            
            representativesField!.SetValue(organization, representativesList);

            return organization;
        }

        public void UpdateDataModel(Organization domain, OrganizationDataModel dataModel)
        {
            dataModel.LegalName = domain.LegalName;
            dataModel.AlternativeName = domain.AlternativeName;
            dataModel.TaxNumber = domain.TaxNumber;
            dataModel.Street = domain.Address.Street;
            dataModel.PostalCode = domain.Address.PostalCode;
            dataModel.City = domain.Address.City;
        }
    }
}
