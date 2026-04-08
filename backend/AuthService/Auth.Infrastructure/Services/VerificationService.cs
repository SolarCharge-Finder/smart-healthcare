namespace Auth.Infrastructure.Services;

using Auth.Application.Interfaces;
using System.Security.Cryptography;

public class VerificationService : IVerificationService
{
    public string GenerateVerificationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}