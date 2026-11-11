using Application.DTO;
using DataModel.Model;
using DataModel.Repository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Service for managing system users
/// US 3.2.5: Administrator can create/update users and assign roles
/// US 3.2.6: Send activation links to users
/// </summary>
public interface IUserManagementService
{
    Task<SystemUserDTO> CreateUserAsync(CreateSystemUserDTO dto, string createdByEmail);
    Task<SystemUserDTO> UpdateUserAsync(UpdateSystemUserDTO dto);
    Task<SystemUserDTO?> GetUserByEmailAsync(string email);
    Task<List<SystemUserDTO>> GetAllUsersAsync();
    Task<bool> ResendActivationEmailAsync(string email);
    Task<ActivationTokenValidationDTO> ValidateActivationTokenAsync(string token);
    Task<bool> ActivateUserAsync(ActivateUserDTO dto);
}

public class UserManagementService : IUserManagementService
{
    private readonly AbsanteeContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserManagementService> _logger;
    private readonly string _applicationUrl;

    public UserManagementService(
        AbsanteeContext context,
        IEmailService emailService,
        ILogger<UserManagementService> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _applicationUrl = configuration["Application:Url"] ?? "http://localhost:4200";
    }

    public async Task<SystemUserDTO> CreateUserAsync(CreateSystemUserDTO dto, string createdByEmail)
    {
        // Check if user already exists
        var existingUser = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant());

        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {dto.Email} already exists");
        }

        // Create domain entity (US 3.2.5: by default deactivated)
        var user = new SystemUser(
            dto.Email.Trim(),
            dto.Name.Trim(),
            dto.Role,
            createdByEmail
        );

        // Generate activation token (US 3.2.5: unique activation link)
        var token = user.GenerateActivationToken();

        // Save to database
        var userDataModel = SystemUserDataModel.FromDomain(user);
        _context.SystemUsers.Add(userDataModel);
        await _context.SaveChangesAsync();

        // Send activation email (US 3.2.5: activation link sent to email)
        var activationLink = $"{_applicationUrl}/activate?email={Uri.EscapeDataString(user.Email)}&token={token}";
        
        try
        {
            await _emailService.SendActivationEmailAsync(user.Email, user.Name, activationLink);
            _logger.LogInformation("User {Email} created with role {Role} by {CreatedBy}. Activation email sent.", user.Email, user.Role, createdByEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send activation email for user {Email}", user.Email);
            // Don't throw - user is created, just log the email failure
        }

        return MapToDTO(userDataModel);
    }

    public async Task<SystemUserDTO> UpdateUserAsync(UpdateSystemUserDTO dto)
    {
        var userDataModel = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant());

        if (userDataModel == null)
        {
            throw new InvalidOperationException($"User with email {dto.Email} not found");
        }

        var user = userDataModel.ToDomain();

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            user.UpdateRole(dto.Role);
            userDataModel.Role = user.Role; // Update tracked entity
            _logger.LogInformation("User {Email} role updated to {Role}", user.Email, dto.Role);
        }

        if (dto.Active.HasValue)
        {
            if (dto.Active.Value)
            {
                // Activate user (US 3.2.5: simple toggle, no activation link)
                userDataModel.Active = true;
                _logger.LogInformation("User {Email} activated", user.Email);
            }
            else
            {
                user.Deactivate();
                userDataModel.Active = false;
                _logger.LogInformation("User {Email} deactivated", user.Email);
            }
        }

        // No need to call Update() - entity is already tracked
        await _context.SaveChangesAsync();

        return MapToDTO(userDataModel);
    }

    public async Task<SystemUserDTO?> GetUserByEmailAsync(string email)
    {
        var userDataModel = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());

        return userDataModel != null ? MapToDTO(userDataModel) : null;
    }

    public async Task<List<SystemUserDTO>> GetAllUsersAsync()
    {
        var users = await _context.SystemUsers
            .OrderBy(u => u.Email)
            .ToListAsync();

        return users.Select(MapToDTO).ToList();
    }

    public async Task<bool> ResendActivationEmailAsync(string email)
    {
        var userDataModel = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());

        if (userDataModel == null)
        {
            return false;
        }

        var user = userDataModel.ToDomain();

        // Generate new token
        var token = user.GenerateActivationToken();
        
        // Update the existing tracked entity instead of creating a new one
        userDataModel.ActivationToken = user.ActivationToken;
        userDataModel.ActivationTokenExpiry = user.ActivationTokenExpiry;
        
        await _context.SaveChangesAsync();

        // Send email
        var activationLink = $"{_applicationUrl}/activate?email={Uri.EscapeDataString(user.Email)}&token={token}";
        
        try
        {
            await _emailService.SendActivationEmailAsync(user.Email, user.Name, activationLink);
            _logger.LogInformation("Activation email resent to {Email}", user.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend activation email to {Email}", user.Email);
            throw;
        }
    }

    public async Task<ActivationTokenValidationDTO> ValidateActivationTokenAsync(string token)
    {
        // Find user by activation token
        var userDataModel = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.ActivationToken == token);

        if (userDataModel == null)
        {
            // Token doesn't exist in database
            // This could mean: token is invalid, token was already used and cleared, or token is fake
            // We cannot distinguish between "already used" and "never existed" without the token
            // So we return a generic "invalid token" message
            return new ActivationTokenValidationDTO
            {
                IsValid = false,
                IsAlreadyActive = false,
                IsExpired = false,
                Message = "Invalid activation token. The token does not exist or has already been used."
            };
        }

        // Check if user is already active (token exists but shouldn't be used)
        if (userDataModel.Active)
        {
            return new ActivationTokenValidationDTO
            {
                IsValid = false,
                IsAlreadyActive = true,
                IsExpired = false,
                Message = "Your account is already active. No activation needed."
            };
        }

        // Check if token is expired
        if (userDataModel.ActivationTokenExpiry == null || DateTime.UtcNow > userDataModel.ActivationTokenExpiry)
        {
            return new ActivationTokenValidationDTO
            {
                IsValid = false,
                IsAlreadyActive = false,
                IsExpired = true,
                Message = "This activation token has expired. Please request a new activation email from your administrator."
            };
        }

        // Token is valid
        return new ActivationTokenValidationDTO
        {
            IsValid = true,
            IsAlreadyActive = false,
            IsExpired = false,
            Message = "Token is valid. Please authenticate to complete activation."
        };
    }

    public async Task<bool> ActivateUserAsync(ActivateUserDTO dto)
    {
        // US 3.2.6 AC: System must confirm authenticated user matches link identity
        // Get user by email (this is the authenticated user's email)
        var userDataModel = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant());

        if (userDataModel == null)
        {
            _logger.LogWarning("Activation attempted for non-existent user {Email}", dto.Email);
            return false;
        }

        // Verify that the token belongs to this user
        // This ensures the authenticated user matches the user identity in the activation link
        if (userDataModel.ActivationToken != dto.ActivationToken)
        {
            _logger.LogWarning("Activation token mismatch for user {Email} - authenticated user doesn't match link identity", dto.Email);
            return false;
        }

        var user = userDataModel.ToDomain();
        var success = user.Activate(dto.ActivationToken);

        if (success)
        {
            // Update the tracked entity
            userDataModel.Active = true;
            userDataModel.ActivationToken = null;
            userDataModel.ActivationTokenExpiry = null;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User {Email} activated successfully", user.Email);
        }
        else
        {
            _logger.LogWarning("Failed activation attempt for user {Email} - invalid or expired token", dto.Email);
        }

        return success;
    }

    private SystemUserDTO MapToDTO(SystemUserDataModel userDataModel)
    {
        return new SystemUserDTO
        {
            Email = userDataModel.Email,
            Name = userDataModel.Name,
            Role = userDataModel.Role,
            Active = userDataModel.Active,
            CreatedAt = userDataModel.CreatedAt,
            CreatedBy = userDataModel.CreatedBy,
            HasPendingActivation = !string.IsNullOrEmpty(userDataModel.ActivationToken),
            ActivationTokenExpiry = userDataModel.ActivationTokenExpiry
        };
    }
}
