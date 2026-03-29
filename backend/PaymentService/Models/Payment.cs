namespace PaymentService.Models;

public class Payment
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public long Amount { get; set; }   // cents
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = "Pending";
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}