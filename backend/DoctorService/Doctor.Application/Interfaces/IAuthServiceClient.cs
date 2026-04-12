using Shared.Contracts.Enums;

namespace Doctor.Application.Interfaces;

public interface IAuthServiceClient
{
    Task GrantRoleAsync(Guid userId, UserRole role);
}
