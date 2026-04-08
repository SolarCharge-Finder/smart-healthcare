namespace Auth.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmail(string email, string token);
}