namespace AdminService.Application.DTOs;

public class DoctorResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}