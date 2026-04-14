using Shared.Contracts.Enums;

namespace Auth.Application.Interfaces;

public interface IUserService
{
    Task UpdateName(Guid userId, string name);

    Task ChangePassword(Guid userId, string currentPassword, string newPassword);

    Task SetRoleAsync(Guid userId, UserRole role);
}
