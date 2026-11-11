namespace Application.DTO;

/// <summary>
/// DTO for user account activation
/// US 3.2.6: User activates account via email link
/// </summary>
public class ActivateUserDTO
{
    public required string Email { get; set; }
    public required string ActivationToken { get; set; }
}
