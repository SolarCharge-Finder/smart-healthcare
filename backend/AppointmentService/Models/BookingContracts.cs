namespace AppointmentService.Models;

public class GuestBookingRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string NicOrPassport { get; set; } = string.Empty;
}

public class CreateAppointmentRequest
{
    public Guid? UserId { get; set; }

    public Guid DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;

    public string HospitalId { get; set; } = string.Empty;

    public string HospitalName { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public DateTime SlotTime { get; set; }

    public GuestBookingRequest? Guest { get; set; }
}

public class PricingResponse
{
    public decimal DoctorFee { get; set; }

    public decimal HospitalFee { get; set; }

    public decimal EChannellingFee { get; set; }

    public decimal Discount { get; set; }

    public decimal TotalFee { get; set; }
}

public class PaymentInitiateRequest
{
    public Guid AppointmentId { get; set; }
}

public class PaymentCallbackRequest
{
    public Guid AppointmentId { get; set; }

    public string PaymentStatus { get; set; } = "SUCCESS";
}
