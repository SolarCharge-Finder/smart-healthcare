namespace PatientService.Application.DTOs;

public class CreatePatientRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
