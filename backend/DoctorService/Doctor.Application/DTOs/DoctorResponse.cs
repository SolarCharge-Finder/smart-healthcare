namespace Doctor.Application.DTOs;

public class DoctorResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string Hospital { get; set; } = string.Empty;

    public bool IsApproved { get; set; }
}