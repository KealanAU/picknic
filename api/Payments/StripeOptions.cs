namespace Picknic.Api.Payments;

/// <summary>
/// Stripe configuration bound from the "Stripe" config section. Payments are
/// optional — if <see cref="SecretKey"/> is empty the checkout endpoint returns
/// 501 and the rest of the app runs fine without Stripe.
/// </summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }

    public string SuccessUrl { get; set; } = "http://localhost:5173/checkout/success";
    public string CancelUrl { get; set; } = "http://localhost:5173/checkout/cancel";

    public bool Enabled => !string.IsNullOrWhiteSpace(SecretKey);
}
