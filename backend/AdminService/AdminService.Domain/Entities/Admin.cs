namespace AdminService.Domain.Entities;

public class Admin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }   // from AuthService

    public string FullName { get; set; } = string.Empty;

    public bool IsApproved { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}