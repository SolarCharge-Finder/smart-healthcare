public class CreatePaymentIntentRequest
{
    public Guid AppointmentId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = "lkr";
}