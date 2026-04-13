namespace Doctor.Application.DTOs;

public class CreateAvailability
{
    public string Hospital { get; set; } = "";

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public bool IsRecurring { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }
}
