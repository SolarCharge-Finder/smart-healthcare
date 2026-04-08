namespace Auth.Infrastructure.Services;

using SendGrid;
using SendGrid.Helpers.Mail;
using Auth.Application.Interfaces;
using Microsoft.Extensions.Configuration;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendVerificationEmail(string email, string token)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        var fromEmail = _config["SendGrid:FromEmail"];
        var fromName = _config["SendGrid:FromName"];

        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("SendGrid API key not configured");

        var client = new SendGridClient(apiKey);

        var encodedToken = Uri.EscapeDataString(token);

        var link = $"http://localhost:3000/verify?token={encodedToken}";

        var msg = new SendGridMessage()
        {
            From = new EmailAddress(fromEmail, fromName),
            Subject = "Verify your SmartHealth account",
            PlainTextContent = $"Click this link to verify your account:\n\n{link}",
            HtmlContent = $"<strong>Click here:</strong> <a href='{link}'>Verify Account</a>"
        };

        msg.AddTo(new EmailAddress(email));

        var response = await client.SendEmailAsync(msg);

        // for debugging - throw if email failed
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            throw new Exception($"Email failed: {body}");
        }
    }
}