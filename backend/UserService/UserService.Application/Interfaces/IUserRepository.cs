namespace UserService.Application.Interfaces;

using UserService.Domain.Entities;

public interface IUserRepository
{
    Task AddAsync(User user);

    Task<User?> GetByIdAsync(Guid id);

    Task<User?> GetByUserIdAsync(Guid userId);

    Task<List<User>> GetAllAsync();

    Task RemoveAsync(User user);

    Task SaveChangesAsync();
}