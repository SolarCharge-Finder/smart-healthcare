namespace PaymentService.Models;

public class CreatePaymentIntentResponse
{
    public Guid PaymentId { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "lkr";
    public string Status { get; set; } = string.Empty;
}
