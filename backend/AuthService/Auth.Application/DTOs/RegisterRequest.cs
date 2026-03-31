namespace Auth.Application.DTOs;

using Auth.Domain.Enums;

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Patient;
}