namespace PaymentService.Models;

public class PaymentPricingOptions
{
    public const string SectionName = "PaymentPricing";

    public long DefaultAmount { get; set; } = 20000;
    public string Currency { get; set; } = "lkr";
    public Dictionary<string, long> DoctorFees { get; set; } = new();
}
