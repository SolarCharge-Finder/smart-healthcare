namespace PaymentService.Models;

public class PaymentPricingOptions
{
    public const string SectionName = "PaymentPricing";

    /// <summary>
    /// Default amount in cents (e.g., 50000 = LKR 500.00)
    /// </summary>
    public long DefaultAmount { get; set; } = 50000;

    /// <summary>
    /// Currency code (e.g., "lkr", "usd")
    /// </summary>
    public string Currency { get; set; } = "lkr";

    /// <summary>
    /// Doctor-specific fees mapped by DoctorId (string GUID) in cents
    /// Example: { "550e8400-e29b-41d4-a716-446655440000": 75000 }
    /// </summary>
    public Dictionary<string, long> DoctorFees { get; set; } = new();

    /// <summary>
    /// When true, uses default pricing if appointment service is unavailable.
    /// ONLY for development; should be false in production.
    /// </summary>
    public bool DevFallbackEnabled { get; set; } = false;
}
