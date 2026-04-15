namespace Doctor.Application.DTOs;

public class FilterOptionsResponse
{
    public List<string> DoctorNames { get; set; } = new List<string>();

    public List<string> Specializations { get; set; } = new List<string>();

    public List<string> Hospitals { get; set; } = new List<string>();
}
