using Domain.Model;

namespace Application.DTO
{
    public class RepresentativeDTO
    {
        public long Id { get; set; }
        public string CitizenId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public long OrganizationId { get; set; }
        public bool IsActive { get; set; }

        public static RepresentativeDTO FromDomain(Representative representative)
        {
            return new RepresentativeDTO
            {
                Id = representative.Id,
                CitizenId = representative.CitizenId,
                Name = representative.Name,
                Nationality = representative.Nationality,
                Email = representative.Email,
                PhoneNumber = representative.PhoneNumber,
                OrganizationId = representative.OrganizationId,
                IsActive = representative.IsActive
            };
        }
    }

    public class CreateRepresentativeDTO
    {
        public string CitizenId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class UpdateRepresentativeDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class RepresentativeWithOrganizationDTO
    {
        public string OrganizationIdentifier { get; set; } = string.Empty;
    }
}
