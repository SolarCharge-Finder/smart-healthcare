namespace Doctor.Domain.Entities;

public class DoctorAvailability
{
    public Guid Id { get; set; }

    public Guid DoctorId { get; set; } // references field: id in Doctor entity

    // currently reduantant, but can be used in future if we want to allow doctors to have availability in multiple hospitals,
    public string Hospital { get; set; } = ""; // string > Guid if we set up a hospital service in fture 
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public bool IsActive { get; set; } = true; // soft temporaray deactivation for a slot

    public bool IsRecurring { get; set; } = false;

    public DayOfWeek? DayOfWeek { get; set; } // used if IsRecurring is true, indicates the day of the week for the recurring slot

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
