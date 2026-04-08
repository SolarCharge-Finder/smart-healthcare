namespace Auth.Application.Interfaces;

using Auth.Application.DTOs;

public interface IAuthService
{
    Task Register(RegisterRequest request);
    Task Verify(string token);
    Task<LoginResponse?> Login(LoginRequest request);
}