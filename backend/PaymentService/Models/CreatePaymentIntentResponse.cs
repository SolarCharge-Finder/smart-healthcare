namespace PaymentService.Models;

public class CreatePaymentIntentResponse
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "lkr";
    public string Status { get; set; } = string.Empty;
}