namespace Application.DTO;

/// <summary>
/// DTO for updating an existing system user
/// US 3.2.5: Administrator can update user roles
/// </summary>
public class UpdateSystemUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Role { get; set; }
    public bool? Active { get; set; }
}
