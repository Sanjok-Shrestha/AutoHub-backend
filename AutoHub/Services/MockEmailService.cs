using Microsoft.Extensions.Logging;

namespace AutoHub.API.Services;

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendConfirmationEmailAsync(string toEmail, string userName, string token)
    {
        var frontendUrl = "http://localhost:5173";
        var confirmLink = $"{frontendUrl}/confirm-email?token={token}";

        // Log to Visual Studio console instead of sending real email
        _logger.LogInformation("""
            
            [MOCK CONFIRMATION EMAIL]
            ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            To: {ToEmail}
            Name: {UserName}
            Token: {Token}
            
            Confirm Link:
            {ConfirmLink}
            
            (In production, this would send via SMTP)
            ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            
            """, toEmail, userName, token, confirmLink);

        return Task.FromResult(true); // Always succeed in dev
    }

    public Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("📧 [MOCK EMAIL] To: {ToEmail} | Subject: {Subject}", toEmail, subject);
        return Task.FromResult(true);
    }
}