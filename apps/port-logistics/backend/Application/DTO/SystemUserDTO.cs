namespace Application.DTO;

/// <summary>
/// DTO for system user display
/// </summary>
public class SystemUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool HasPendingActivation { get; set; }
    public DateTime? ActivationTokenExpiry { get; set; }
}
