namespace Auth.Application.Interfaces;

using Auth.Application.DTOs;
using Auth.Domain.Entities;

public interface IAuthService
{
    Task Register(RegisterRequest request);
    Task<string?> Login(LoginRequest request);
}