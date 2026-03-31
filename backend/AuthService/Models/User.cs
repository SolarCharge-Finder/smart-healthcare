namespace AuthService.Models;

public enum UserRole
{
    Patient,
    Doctor,
    Admin
}
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Patient;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}