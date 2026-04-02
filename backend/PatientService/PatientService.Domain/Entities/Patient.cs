namespace PatientService.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }              // internal DB id

    public Guid UserId { get; set; }          // from AuthService

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}