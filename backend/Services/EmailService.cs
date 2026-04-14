using System.Net;
using System.Net.Mail;

namespace NutrientInsight.Api.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmail(string toEmail, string resetToken)
    {
        var frontendUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{frontendUrl}/reset-password?token={resetToken}";

        var smtpHost = _configuration["Smtp:Host"];
        var smtpPort = _configuration.GetValue<int?>("Smtp:Port");
        var smtpUser = _configuration["Smtp:User"];
        var smtpPass = _configuration["Smtp:Pass"];
        var smtpFrom = _configuration["Smtp:From"];

        if (string.IsNullOrEmpty(smtpHost) || !smtpPort.HasValue)
        {
            _logger.LogWarning("SMTP not configured. Reset link: {ResetLink}", resetLink);
            return;
        }

        var subject = "VitalNexus - Reset Your Password";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #667eea;'>Reset Your Password</h2>
                    <p>You requested to reset your password for VitalNexus.</p>
                    <p>Click the link below to reset your password. This link will expire in 60 minutes.</p>
                    <p style='margin: 30px 0;'>
                        <a href='{resetLink}' style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 12px 24px; text-decoration: none; border-radius: 8px; display: inline-block;'>
                            Reset Password
                        </a>
                    </p>
                    <p style='color: #666; font-size: 14px;'>If you didn't request this, you can safely ignore this email.</p>
                    <p style='color: #666; font-size: 14px;'>Link: {resetLink}</p>
                </div>
            </body>
            </html>
        ";

        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress(smtpFrom ?? smtpUser ?? "noreply@vitalnexus.com");
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var smtp = new SmtpClient(smtpHost, smtpPort.Value);
            smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);
            smtp.EnableSsl = true;

            await smtp.SendMailAsync(message);
            _logger.LogInformation("Password reset email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email");
            throw;
        }
    }
}