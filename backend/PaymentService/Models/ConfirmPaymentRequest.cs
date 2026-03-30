namespace PaymentService.Models;

public class ConfirmPaymentRequest
{
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
}
