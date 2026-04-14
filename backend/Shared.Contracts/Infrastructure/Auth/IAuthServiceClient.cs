using Shared.Contracts.Enums;

namespace Shared.Contracts.Infrastructure.Auth;

public interface IAuthServiceClient
{
    Task GrantRoleAsync(Guid userId, UserRole role);
}
