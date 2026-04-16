using Auth.Application.Interfaces;
using Auth.Domain.Entities;

namespace AuthService.Tests.Fakes;

public class FakeTokenService : ITokenService
{
    public string GenerateToken(User user)
    {
        // return something predictable for tests
        return $"fake-token-for-{user.Id}";
    }

    public string GenerateInternalToken(string serviceName)
    {
        return $"internal-token-for-{serviceName}";
    }
}