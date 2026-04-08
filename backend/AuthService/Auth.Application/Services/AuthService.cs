namespace Auth.Application.Services;

using Auth.Application.Interfaces;
using Auth.Application.DTOs;
using Auth.Domain.Entities;
using Auth.Domain.Enums;

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
            throw new InvalidOperationException("User already exists");

        if (request.Role == UserRole.Doctor)
            throw new InvalidOperationException("Cannot self-register as doctor");

        if (request.Role == UserRole.Admin)
            throw new InvalidOperationException("Cannot self-register as admin");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(user);
        await _repo.SaveChangesAsync();
    }

    public async Task<LoginResponse?> Login(LoginRequest request)
    {
        var email = request.Email.ToLower().Trim();

        var user = await _repo.GetByEmailAsync(email);

        if (user == null) throw new UnauthorizedAccessException("Invalid credentials"); ;

        var valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!valid) throw new UnauthorizedAccessException("Invalid credentials");

        var token = _tokenService.GenerateToken(user);

        var response = new LoginResponse
        {
            Token = token,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString()
        };

        return response;
    }
}