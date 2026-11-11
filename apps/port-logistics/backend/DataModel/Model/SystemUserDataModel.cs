using Domain.Model;

namespace DataModel.Model;

/// <summary>
/// Data model for SystemUser entity
/// Maps Domain.Model.SystemUser to database table
/// </summary>
public class SystemUserDataModel
{
    public string Email { get; set; } = string.Empty; // Primary Key
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string? ActivationToken { get; set; }
    public DateTime? ActivationTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Convert to Domain entity
    /// </summary>
    public SystemUser ToDomain()
    {
        var user = new SystemUser(Email, Name, Role, CreatedBy);
        
        // Use reflection to set private fields for loaded entities
        var type = typeof(SystemUser);
        
        type.GetField("_active", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(user, Active);
        
        type.GetField("_activationToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(user, ActivationToken);
        
        type.GetField("_activationTokenExpiry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(user, ActivationTokenExpiry);
        
        type.GetField("_createdAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(user, CreatedAt);

        return user;
    }

    /// <summary>
    /// Create from Domain entity
    /// </summary>
    public static SystemUserDataModel FromDomain(SystemUser user)
    {
        return new SystemUserDataModel
        {
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            Active = user.Active,
            ActivationToken = user.ActivationToken,
            ActivationTokenExpiry = user.ActivationTokenExpiry,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy
        };
    }
}
