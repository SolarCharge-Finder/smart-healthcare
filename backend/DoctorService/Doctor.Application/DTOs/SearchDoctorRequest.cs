namespace Doctor.Application.DTOs;

public class SearchDoctorsRequest
{
    public string? Name { get; set; }
    public string? Specialization { get; set; }
    public string? Hospital { get; set; }
    public DateTime? Date { get; set; }
}
