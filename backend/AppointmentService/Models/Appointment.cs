namespace AppointmentService.Models;

public class Appointment
{
    public Guid Id { get; set; }

    public Guid PatientId { get; set; }

    public Guid? UserId { get; set; }

    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string HospitalId { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public DateTime SlotTime { get; set; }

    public DateTime AppointmentDate { get; set; }

    public int AppointmentNumber { get; set; }

    public string BookingReferenceId { get; set; } = string.Empty;

    public decimal DoctorFee { get; set; }

    public decimal HospitalFee { get; set; }

    public decimal EChannellingFee { get; set; }

    public decimal Discount { get; set; }

    public decimal TotalFee { get; set; }

    public Guid? GuestUserId { get; set; }

    public GuestUser? GuestUser { get; set; }

    public string Status { get; set; } = "PENDING_PAYMENT";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
