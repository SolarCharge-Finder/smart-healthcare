namespace Auth.Application.Services;

using Auth.Application.Interfaces;
using Auth.Application.DTOs;
using Auth.Domain.Entities;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;

    public AuthService(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task Register(RegisterRequest request)
    {
        var exists = await _repo.ExistsByEmailAsync(request.Email);

        if (exists)
            throw new Exception("User already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(user);
        await _repo.SaveChangesAsync();
    }

    public async Task<User?> Login(LoginRequest request)
    {
        var user = await _repo.GetByEmailAsync(request.Email);

        if (user == null) return null;

        return BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)
            ? user
            : null;
    }
}