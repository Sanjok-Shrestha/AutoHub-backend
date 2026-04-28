namespace AutoHub.API.Services;

public interface IEmailService
{
    Task<bool> SendConfirmationEmailAsync(string toEmail, string userName, string token);
    Task<bool> SendEmailAsync(string toEmail, string subject, string body);
}