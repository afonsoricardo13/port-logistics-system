using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Service for sending emails
/// US 3.2.6: Send activation links to users
/// </summary>
public interface IEmailService
{
    Task SendActivationEmailAsync(string toEmail, string userName, string activationLink);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUsername;
    private readonly string? _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _applicationUrl;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        _smtpHost = _configuration["Email:SmtpHost"] ?? "frodo.dei.isep.ipp.pt";
        _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "25");
        _smtpUsername = _configuration["Email:SmtpUsername"]; // Optional for authenticated SMTP
        _smtpPassword = _configuration["Email:SmtpPassword"]; // Optional for authenticated SMTP
        _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@portlogistics.com";
        _fromName = _configuration["Email:FromName"] ?? "Port Logistics System";
        _applicationUrl = _configuration["Application:Url"] ?? "http://localhost:4200";
    }

    public async Task SendActivationEmailAsync(string toEmail, string userName, string activationLink)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Activate Your Port Logistics Account",
                Body = BuildActivationEmailBody(userName, activationLink),
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(toEmail));

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = !string.IsNullOrEmpty(_smtpUsername), // Enable SSL only if using authentication (e.g., Gmail)
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = !string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword)
                    ? new NetworkCredential(_smtpUsername, _smtpPassword)
                    : null // ISEP SMTP doesn't require authentication
            };

            await smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation("Activation email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send activation email to {Email}", toEmail);
            throw new InvalidOperationException($"Failed to send activation email: {ex.Message}", ex);
        }
    }

    private string BuildActivationEmailBody(string userName, string activationLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #004085; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 30px; }}
        .button {{ 
            display: inline-block; 
            padding: 12px 30px; 
            background-color: #007bff; 
            color: white; 
            text-decoration: none; 
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Port Logistics System</h1>
        </div>
        <div class=""content"">
            <h2>Welcome, {userName}!</h2>
            <p>An administrator has created an account for you in the Port Logistics System.</p>
            <p>To activate your account and gain access to the system, please click the button below:</p>
            <p style=""text-align: center;"">
                <a href=""{activationLink}"" class=""button"">Activate Account</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style=""word-break: break-all; background-color: white; padding: 10px; border: 1px solid #ddd;"">
                {activationLink}
            </p>
            <p><strong>Important:</strong> This activation link will expire in 7 days.</p>
            <p>If you did not expect this email or have any questions, please contact your administrator.</p>
        </div>
        <div class=""footer"">
            <p>© 2025 Port Logistics System. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
    }
}
