namespace Application.DTO;

/// <summary>
/// DTO for activation token validation result
/// Used to check token status before requiring authentication
/// </summary>
public class ActivationTokenValidationDTO
{
    /// <summary>
    /// Is the token valid and not expired?
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Is the user already active (token already used)?
    /// </summary>
    public bool IsAlreadyActive { get; set; }
    
    /// <summary>
    /// Is the token expired?
    /// </summary>
    public bool IsExpired { get; set; }
    
    /// <summary>
    /// Message explaining the token status
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
