namespace Auth.Application.Interfaces;

using Auth.Domain.Entities;

public interface ITokenService
{
    string GenerateToken(User user);

    string GenerateInternalToken(string serviceName);
}
