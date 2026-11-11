namespace Domain.Model;

/// <summary>
/// Domain entity for system users
/// US 3.2.5: Administrator can assign/update internal roles
/// US 3.2.6: Users receive activation link for first access
/// </summary>
public class SystemUser
{
    private string _email = string.Empty;
    private string _name = string.Empty;
    private string _role = string.Empty;
    private bool _active;
    private string? _activationToken;
    private DateTime? _activationTokenExpiry;
    private DateTime _createdAt;
    private string _createdBy = string.Empty;

    /// <summary>
    /// Protected constructor for EF Core
    /// </summary>
    protected SystemUser() { }

    /// <summary>
    /// Create new system user
    /// AC: Users identified by IAM-provided attributes (email, name)
    /// AC: By default, users are set to "deactivated" status (US 3.2.5)
    /// </summary>
    public SystemUser(string email, string name, string role, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        if (!IsValidRole(role))
            throw new ArgumentException($"Invalid role: {role}", nameof(role));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy cannot be empty", nameof(createdBy));

        _email = email.Trim().ToLowerInvariant();
        _name = name.Trim();
        _role = role;
        _active = false; // AC: By default deactivated (US 3.2.5)
        _createdAt = DateTime.UtcNow;
        _createdBy = createdBy;
    }

    public string Email => _email;
    public string Name => _name;
    public string Role => _role;
    public bool Active => _active;
    public string? ActivationToken => _activationToken;
    public DateTime? ActivationTokenExpiry => _activationTokenExpiry;
    public DateTime CreatedAt => _createdAt;
    public string CreatedBy => _createdBy;

    /// <summary>
    /// Update user role
    /// AC: Administrator can update internal role(s)
    /// </summary>
    public void UpdateRole(string newRole)
    {
        if (string.IsNullOrWhiteSpace(newRole))
            throw new ArgumentException("Role cannot be empty", nameof(newRole));

        if (!IsValidRole(newRole))
            throw new ArgumentException($"Invalid role: {newRole}", nameof(newRole));

        _role = newRole;
    }

    /// <summary>
    /// Generate activation token
    /// AC: When authorizing user for first time, unique activation link is sent
    /// </summary>
    public string GenerateActivationToken()
    {
        _activationToken = Guid.NewGuid().ToString("N");
        _activationTokenExpiry = DateTime.UtcNow.AddDays(7); // Token valid for 7 days
        return _activationToken;
    }

    /// <summary>
    /// Activate user with token validation
    /// US 3.2.6: System confirms authenticated user matches link identity
    /// </summary>
    public bool Activate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (_activationToken != token)
            return false;

        if (_activationTokenExpiry == null || DateTime.UtcNow > _activationTokenExpiry)
            return false;

        _active = true;
        _activationToken = null; // Clear token after use
        _activationTokenExpiry = null;
        return true;
    }

    /// <summary>
    /// Deactivate user
    /// Not explicitly required by US 3.2.5/3.2.6 but kept for administrative purposes
    /// </summary>
    public void Deactivate()
    {
        _active = false;
    }

    /// <summary>
    /// Reactivate user (generates new activation token)
    /// Not explicitly required by US 3.2.5/3.2.6 but kept for consistency with other domain entities
    /// </summary>
    public string Reactivate()
    {
        _active = false; // Requires new activation
        return GenerateActivationToken();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidRole(string role)
    {
        var validRoles = new[]
        {
            "Administrator",
            "PortAuthorityOfficer",
            "LogisticsOperator",
            "PortOperationsSupervisor",
            "ShippingAgentRepresentative"
        };

        return validRoles.Contains(role);
    }
}
