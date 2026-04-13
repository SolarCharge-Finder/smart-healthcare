using Shared.Contracts.Enums;
using Shared.Contracts.Infrastructure.Auth;

namespace PatientService.Tests.Fakes;

public class FakeAuthServiceClient : IAuthServiceClient
{
    public static UserRole? LastAssignedRole { get; private set; }

    public Task GrantRoleAsync(Guid userId, UserRole role)
    {
        LastAssignedRole = role;
        return Task.CompletedTask;
    }
}
