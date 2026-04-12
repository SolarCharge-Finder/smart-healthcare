namespace DoctorService.Tests.Fakes;

using Doctor.Application.Interfaces;
using Shared.Contracts.Enums;

public class FakeAuthServiceClient : IAuthServiceClient
{
    public Task GrantRoleAsync(Guid userId, UserRole role)
    {
        // do nothing (mock behavior)
        return Task.CompletedTask;
    }
}