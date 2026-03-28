namespace AuthService.DTOs;

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Patient";
}