namespace DataModel.Model
{
    public class RepresentativeDataModel
    {
        public long Id { get; set; }
        public string CitizenId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public long OrganizationId { get; set; }
        public bool IsActive { get; set; }

        // Navigation property
        public virtual OrganizationDataModel? Organization { get; set; }
    }
}
