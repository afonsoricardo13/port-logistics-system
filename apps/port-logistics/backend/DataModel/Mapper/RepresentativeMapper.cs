using DataModel.Model;
using Domain.Model;

namespace DataModel.Mapper
{
    public class RepresentativeMapper
    {
        public RepresentativeDataModel ToDataModel(Representative domain)
        {
            return new RepresentativeDataModel
            {
                Id = domain.Id,
                CitizenId = domain.CitizenId,
                Name = domain.Name,
                Nationality = domain.Nationality,
                Email = domain.Email,
                PhoneNumber = domain.PhoneNumber,
                OrganizationId = domain.OrganizationId,
                IsActive = domain.IsActive
            };
        }

        public Representative ToDomain(RepresentativeDataModel dataModel)
        {
            // Use reflection to create domain object with private constructor
            var representative = (Representative)System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(typeof(Representative));

            typeof(Representative).GetProperty("Id")!
                .SetValue(representative, dataModel.Id);
            typeof(Representative).GetProperty("CitizenId")!
                .SetValue(representative, dataModel.CitizenId);
            typeof(Representative).GetProperty("Name")!
                .SetValue(representative, dataModel.Name);
            typeof(Representative).GetProperty("Nationality")!
                .SetValue(representative, dataModel.Nationality);
            typeof(Representative).GetProperty("Email")!
                .SetValue(representative, dataModel.Email);
            typeof(Representative).GetProperty("PhoneNumber")!
                .SetValue(representative, dataModel.PhoneNumber);
            typeof(Representative).GetProperty("OrganizationId")!
                .SetValue(representative, dataModel.OrganizationId);
            typeof(Representative).GetProperty("IsActive")!
                .SetValue(representative, dataModel.IsActive);

            return representative;
        }

        public void UpdateDataModel(Representative domain, RepresentativeDataModel dataModel)
        {
            dataModel.Name = domain.Name;
            dataModel.Nationality = domain.Nationality;
            dataModel.Email = domain.Email;
            dataModel.PhoneNumber = domain.PhoneNumber;
            dataModel.IsActive = domain.IsActive;
        }
    }
}
