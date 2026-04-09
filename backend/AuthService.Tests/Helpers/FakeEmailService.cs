namespace AuthService.Tests.Helpers;

using Auth.Application.Interfaces;
public class FakeEmailService : IEmailService
{
    public static Dictionary<string, string> LastVerificationToken = new();
    public static Dictionary<string, string> LastPasswordResetToken = new();

    public Task SendVerificationEmail(string email, string token)
    {
        LastVerificationToken[email] = token;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmail(string email, string token)
    {
        LastPasswordResetToken[email] = token;
        return Task.CompletedTask;
    }
}