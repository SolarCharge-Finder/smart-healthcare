using Auth.Application.Interfaces;

namespace AuthService.Tests.Fakes;

public class FakeEmailService : IEmailService
{
    public List<(string Email, string Token)> VerificationEmails { get; } = new();
    public List<(string Email, string Token)> PasswordResetEmails { get; } = new();

    public Task SendVerificationEmail(string email, string token)
    {
        VerificationEmails.Add((email, token));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmail(string email, string token)
    {
        PasswordResetEmails.Add((email, token));
        return Task.CompletedTask;
    }
}
