namespace Doctor.Application.DTOs;

public class CreateDoctorRequest
{
    public string FullName { get; set; } = "";
    public string Specialization { get; set; } = "";
    public string Hospital { get; set; } = "";
}
