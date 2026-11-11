namespace Application.DTO;

/// <summary>
/// DTO for creating a new system user
/// US 3.2.5: Administrator can create users and assign roles
/// </summary>
public class CreateSystemUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
