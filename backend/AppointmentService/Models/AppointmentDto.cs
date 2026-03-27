namespace AppointmentService.Models;

public class AppointmentDto
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public DateTime SlotTime { get; set; }

    public string Status { get; set; } = "Booked";

    public DateTime CreatedAt { get; set; }
}
