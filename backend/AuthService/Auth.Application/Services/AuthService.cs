namespace Auth.Application.Services;

using Auth.Application.Interfaces;
using Auth.Application.DTOs;
using Auth.Domain.Entities;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository repo, ITokenService tokenService)
    {
        _repo = repo;
        _tokenService = tokenService;
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

    public async Task<string?> Login(LoginRequest request)
    {
        var user = await _repo.GetByEmailAsync(request.Email);

        if (user == null) return null;

        var valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!valid) return null;

        return _tokenService.GenerateToken(user);
    }
}