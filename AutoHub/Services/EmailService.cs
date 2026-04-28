using System.Net;
using System.Net.Mail;

namespace AutoHub.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendConfirmationEmailAsync(string toEmail, string userName, string token)
    {
        try
        {
            var smtp = _config.GetSection("SmtpSettings");
            var frontend = _config["AppSettings:FrontendUrl"] ?? "http://localhost:5173";
            var link = $"{frontend}/confirm-email?token={Uri.EscapeDataString(token)}";

            var subject = "Confirm Your Email - AutoHub";
            var body = $@"
                <html><body style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:20px;'>
                <h2 style='color:#2c3e50;'>Welcome to AutoHub! 🚗</h2>
                <p>Hi {userName},</p>
                <p>Thank you for registering. Please confirm your email:</p>
                <div style='text-align:center;margin:30px 0;'>
                    <a href='{link}' style='background:#3498db;color:white;padding:12px 30px;text-decoration:none;border-radius:5px;font-weight:bold;'>
                        Confirm Email
                    </a>
                </div>
                <p><small>Link expires in 24 hours.</small></p>
                </body></html>";

            return await SendEmailAsync(toEmail, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var smtp = _config.GetSection("SmtpSettings");
            using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"] ?? "587"))
            {
                Credentials = new NetworkCredential(smtp["Username"], smtp["Password"]),
                EnableSsl = bool.Parse(smtp["EnableSsl"] ?? "true"),
                Timeout = 30000
            };

            var msg = new MailMessage
            {
                From = new MailAddress(smtp["FromEmail"] ?? smtp["Username"], smtp["FromName"] ?? "AutoHub"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);

            await client.SendMailAsync(msg);
            _logger.LogInformation("Email sent to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP error for {Email}", toEmail);
            return false;
        }
    }
}