namespace Doctor.Application.DTOs;

public class AvailabilityResponse
{
    public Guid Id { get; set; }

    public Guid DoctorId { get; set; }

    public string Hospital { get; set; } = "";

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public bool IsActive { get; set; }

    public bool IsRecurring { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }
}
