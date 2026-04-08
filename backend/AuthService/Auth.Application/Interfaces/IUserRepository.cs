namespace Auth.Application.Interfaces;

using Auth.Domain.Entities;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email);
    Task AddAsync(User user);
    Task<User?> GetByEmailAsync(string email);
    Task SaveChangesAsync();
}