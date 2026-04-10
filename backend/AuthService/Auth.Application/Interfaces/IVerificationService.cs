namespace Auth.Application.Interfaces;

public interface IVerificationService
{
    string GenerateVerificationToken();
}
