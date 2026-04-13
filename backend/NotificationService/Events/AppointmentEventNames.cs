namespace NotificationService.Events;

public static class AppointmentEventNames
{
    public const string Created = "appointment.created";
    public const string Confirmed = "appointment.confirmed";
    public const string PaymentConfirmed = "appointment.payment-confirmed";
    public const string Cancelled = "appointment.cancelled";
    public const string Declined = "appointment.declined";
}
