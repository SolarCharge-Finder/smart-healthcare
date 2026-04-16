namespace AdminService.Tests.Fakes;

using Shared.Contracts.Enums;
using Shared.Contracts.Infrastructure.Auth;

public class FakeAuthServiceClient : IAuthServiceClient
{
    public Task GrantRoleAsync(Guid userId, UserRole role)
    {
        // do nothing (mock behavior)
        return Task.CompletedTask;
    }
}
