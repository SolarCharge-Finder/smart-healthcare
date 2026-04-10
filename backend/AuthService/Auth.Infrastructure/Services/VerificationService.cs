namespace Auth.Infrastructure.Services;

using System.Security.Cryptography;

using Auth.Application.Interfaces;

public class VerificationService : IVerificationService
{
    public string GenerateVerificationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
