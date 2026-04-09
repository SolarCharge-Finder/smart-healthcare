namespace AuthService.Tests;

using Auth.Application.Interfaces;
public class FakeEmailService : IEmailService
{
    public static string? LastSentToken;

    public Task SendVerificationEmail(string email, string token)
    {
        LastSentToken = token;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmail(string email, string token)
    {
        LastSentToken = token;
        return Task.CompletedTask;
    }
}