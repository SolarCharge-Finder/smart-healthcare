using Auth.Application.Interfaces;
using Auth.Domain.Entities;

using Shared.Contracts.Enums;

namespace Auth.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task UpdateName(Guid userId, string name)
    {
        var user = await _repo.GetByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Name cannot be empty");
        }

        user.Name = name.Trim();

        await _repo.SaveChangesAsync();
    }

    public async Task ChangePassword(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _repo.GetByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // verify current password
        var isValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);

        if (!isValid)
        {
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // basic validation
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new InvalidOperationException("New password cannot be empty");
        }

        if (newPassword.Length < 6)
        {
            throw new InvalidOperationException("Password must be at least 6 characters");
        }

        // update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        await _repo.SaveChangesAsync();
    }

    public async Task SetRoleAsync(Guid userId, UserRole role)
    {
        var user = await _repo.GetByIdAsync(userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        user.Role = role;

        await _repo.SaveChangesAsync();
    }
}
