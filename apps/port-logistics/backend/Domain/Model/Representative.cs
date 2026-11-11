using System;

namespace Domain.Model
{
    /// <summary>
    /// Representative entity representing an individual authorized to act on behalf of a shipping agent organization.
    /// US 2.2.5 and 2.2.6: Representatives must have name, citizen ID, nationality, email, and phone number.
    /// Each representative is associated with exactly one organization.
    /// </summary>
    public class Representative
    {
        public long Id { get; private set; }
        public string CitizenId { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string Nationality { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PhoneNumber { get; private set; } = string.Empty;
        public long OrganizationId { get; private set; }
        public bool IsActive { get; private set; }

        private Representative()
        {
            // Required by EF Core
        }

        public Representative(
            string citizenId,
            string name,
            string nationality,
            string email,
            string phoneNumber,
            long organizationId)
        {
            if (string.IsNullOrWhiteSpace(citizenId))
                throw new ArgumentException("Citizen ID cannot be null or empty.", nameof(citizenId));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(nationality))
                throw new ArgumentException("Nationality cannot be null or empty.", nameof(nationality));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));

            // OrganizationId can be 0 temporarily when creating organization with initial representative
            // EF Core will set the correct value during persistence
            if (organizationId < 0)
                throw new ArgumentException("Organization ID cannot be negative.", nameof(organizationId));

            CitizenId = citizenId;
            Name = name;
            Nationality = nationality;
            Email = email;
            PhoneNumber = phoneNumber;
            OrganizationId = organizationId;
            IsActive = true;
        }

        public void UpdateDetails(
            string name,
            string nationality,
            string email,
            string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(nationality))
                throw new ArgumentException("Nationality cannot be null or empty.", nameof(nationality));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));

            Name = name;
            Nationality = nationality;
            Email = email;
            PhoneNumber = phoneNumber;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Reactivate()
        {
            IsActive = true;
        }
    }
}
