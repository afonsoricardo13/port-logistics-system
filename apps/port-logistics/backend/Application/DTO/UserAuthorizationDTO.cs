namespace Application.DTO;

/// <summary>
/// DTO for user authorization information
/// US 3.2.2: Automatically load internal authorization role after authentication
/// </summary>
public class UserAuthorizationDTO
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string? Message { get; set; } // For access denied messages
}
