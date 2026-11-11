namespace Application.DTO
{
    /// <summary>
    /// User profile with role information returned after authentication
    /// US 3.2.2: Return assigned internal role after external IAM authentication
    /// US 3.2.4: Include permissions for frontend and backend authorization
    /// </summary>
    public class UserProfileDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
        public string[] Permissions { get; set; } = Array.Empty<string>();
    }
}
