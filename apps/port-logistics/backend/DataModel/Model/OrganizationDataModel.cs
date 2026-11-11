using System.Collections.Generic;

namespace DataModel.Model
{
    public class OrganizationDataModel
    {
        public long Id { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string AlternativeName { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        
        // Address as owned entity (value object)
        public string Street { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<RepresentativeDataModel> Representatives { get; set; } 
            = new List<RepresentativeDataModel>();
    }
}
