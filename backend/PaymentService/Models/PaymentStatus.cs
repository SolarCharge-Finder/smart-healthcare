namespace PaymentService.Models;

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string RequiresAction = "RequiresAction";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}