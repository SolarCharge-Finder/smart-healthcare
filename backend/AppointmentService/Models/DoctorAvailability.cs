namespace AppointmentService.Models;

public class DoctorAvailability
{
    public Guid Id { get; set; }

    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string HospitalId { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;

    public DateTime SlotTime { get; set; }

    public decimal DoctorFee { get; set; }

    public decimal HospitalFee { get; set; }

    public decimal EChannellingFee { get; set; }

    public decimal Discount { get; set; }
}
