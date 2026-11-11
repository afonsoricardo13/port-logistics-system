using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Model
{
    /// <summary>
    /// Organization entity representing a shipping agent organization.
    /// US 2.2.5: Shipping agent organizations must have identifier, legal and alternative names, 
    /// address, and tax number.
    /// </summary>
    public class Organization
    {
        public long Id { get; private set; }
        public string Identifier { get; private set; } = string.Empty;
        public string LegalName { get; private set; } = string.Empty;
        public string AlternativeName { get; private set; } = string.Empty;
        public string TaxNumber { get; private set; } = string.Empty;
        public Address Address { get; private set; }
        
        private readonly List<Representative> _representatives = new();
        public IReadOnlyCollection<Representative> Representatives => _representatives.AsReadOnly();

        private Organization()
        {
            // Required by EF Core
            Address = new Address("", "", "");
        }

        public Organization(
            string identifier,
            string legalName,
            string alternativeName,
            string taxNumber,
            Address address,
            Representative initialRepresentative)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));

            if (string.IsNullOrWhiteSpace(legalName))
                throw new ArgumentException("Legal name cannot be null or empty.", nameof(legalName));

            if (string.IsNullOrWhiteSpace(alternativeName))
                throw new ArgumentException("Alternative name cannot be null or empty.", nameof(alternativeName));

            if (string.IsNullOrWhiteSpace(taxNumber))
                throw new ArgumentException("Tax number cannot be null or empty.", nameof(taxNumber));

            if (address == null)
                throw new ArgumentNullException(nameof(address));

            if (initialRepresentative == null)
                throw new ArgumentNullException(nameof(initialRepresentative), 
                    "Organization must have at least one representative at creation.");

            Identifier = identifier;
            LegalName = legalName;
            AlternativeName = alternativeName;
            TaxNumber = taxNumber;
            Address = address;
            
            _representatives.Add(initialRepresentative);
        }

        public void UpdateDetails(string legalName, string alternativeName, string taxNumber, Address address)
        {
            if (string.IsNullOrWhiteSpace(legalName))
                throw new ArgumentException("Legal name cannot be null or empty.", nameof(legalName));

            if (string.IsNullOrWhiteSpace(alternativeName))
                throw new ArgumentException("Alternative name cannot be null or empty.", nameof(alternativeName));

            if (string.IsNullOrWhiteSpace(taxNumber))
                throw new ArgumentException("Tax number cannot be null or empty.", nameof(taxNumber));

            if (address == null)
                throw new ArgumentNullException(nameof(address));

            LegalName = legalName;
            AlternativeName = alternativeName;
            TaxNumber = taxNumber;
            Address = address;
        }

        public void AddRepresentative(Representative representative)
        {
            if (representative == null)
                throw new ArgumentNullException(nameof(representative));

            if (_representatives.Any(r => r.Email == representative.Email))
                throw new InvalidOperationException(
                    $"A representative with email {representative.Email} already exists in this organization.");

            _representatives.Add(representative);
        }

        public void RemoveRepresentative(Representative representative)
        {
            if (representative == null)
                throw new ArgumentNullException(nameof(representative));

            if (_representatives.Count <= 1)
                throw new InvalidOperationException(
                    "Cannot remove the last representative. Organization must have at least one representative.");

            _representatives.Remove(representative);
        }
    }
}
