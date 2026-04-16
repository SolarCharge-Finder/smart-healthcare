using Shared.Contracts.Enums;

namespace Auth.Application.Interfaces;

using Auth.Application.DTOs;

public interface IUserService
{
    Task<UserResponse?> GetById(Guid userid);
    Task UpdateName(Guid userId, string name);

    Task ChangePassword(Guid userId, string currentPassword, string newPassword);

    Task SetRoleAsync(Guid userId, UserRole role);
}
