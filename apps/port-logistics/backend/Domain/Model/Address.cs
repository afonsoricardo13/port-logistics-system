using System;

namespace Domain.Model
{
    /// <summary>
    /// Address value object representing a physical address.
    /// </summary>
    public class Address : IEquatable<Address>
    {
        public string Street { get; private set; } = string.Empty;
        public string PostalCode { get; private set; } = string.Empty;
        public string City { get; private set; } = string.Empty;

        private Address()
        {
            // Required by EF Core
        }

        public Address(string street, string postalCode, string city)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Street cannot be null or empty.", nameof(street));

            if (string.IsNullOrWhiteSpace(postalCode))
                throw new ArgumentException("Postal code cannot be null or empty.", nameof(postalCode));

            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be null or empty.", nameof(city));

            Street = street;
            PostalCode = postalCode;
            City = city;
        }

        public bool Equals(Address? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Street == other.Street 
                && PostalCode == other.PostalCode 
                && City == other.City;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Address);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Street, PostalCode, City);
        }

        public override string ToString()
        {
            return $"{Street}, {PostalCode} {City}";
        }
    }
}
