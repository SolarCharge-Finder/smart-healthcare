namespace Auth.Domain.Entities;

using Shared.Contracts.Enums;

public class PendingUser
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Undefined;
    public string VerificationToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
