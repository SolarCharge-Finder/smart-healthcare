namespace UserService.Application.Services;

using UserService.Application.Interfaces;
using UserService.Application.DTOs;
using UserService.Domain.Entities;

public class UserServiceImplementation : IUserService
{
    private readonly IUserRepository _repo;

    public UserServiceImplementation(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<Guid> CreateUser(Guid userId, CreateUserRequest request)
    {
        var existing = await _repo.GetByUserIdAsync(userId);

        if (existing != null)
            throw new Exception("User profile already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = request.FullName,
            Email = request.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(user);
        await _repo.SaveChangesAsync();

        return user.Id;
    }

    public async Task<UserResponse?> GetByUserId(Guid userId)
    {
        var user = await _repo.GetByUserIdAsync(userId);

        if (user == null || !user.IsActive)
            return null;

        return Map(user);
    }

    public async Task<UserResponse?> GetById(Guid id)
    {
        var user = await _repo.GetByIdAsync(id);

        if (user == null || !user.IsActive)
            return null;

        return Map(user);
    }

    public async Task<List<UserResponse>> GetAll()
    {
        var users = await _repo.GetAllAsync();

        return users
            .Where(u => u.IsActive)
            .Select(Map)
            .ToList();
    }

    public async Task UpdateUser(Guid userId, UpdateUserRequest request)
    {
        var user = await _repo.GetByUserIdAsync(userId);

        if (user == null)
            throw new Exception("User not found");

        if (!user.IsActive)
            throw new Exception("User is deactivated");

        user.FullName = request.FullName;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
    }

    public async Task DeactivateUser(Guid userId)
    {
        var user = await _repo.GetByUserIdAsync(userId);

        if (user == null)
            throw new Exception("User not found");

        if (!user.IsActive)
            throw new Exception("User already deactivated");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
    }

    public async Task DeleteUser(Guid id)
    {
        var user = await _repo.GetByIdAsync(id);

        if (user == null)
            throw new Exception("User not found");

        await _repo.RemoveAsync(user);
        await _repo.SaveChangesAsync();
    }

    // mapper
    private static UserResponse Map(User u) => new()
    {
        Id = u.Id,
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email
    };
}