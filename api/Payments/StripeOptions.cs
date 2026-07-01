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

    /// <summary>
    /// Plan -> Stripe Price ID (price_...). When a plan has one, checkout uses it
    /// instead of the ad-hoc inline price. Bound from "Stripe:Prices:{plan}".
    /// </summary>
    public Dictionary<string, string> Prices { get; set; } = new();

    public string SuccessUrl { get; set; } = "http://localhost:3000/checkout/success?session_id={CHECKOUT_SESSION_ID}";
    public string CancelUrl { get; set; } = "http://localhost:3000/checkout/cancel";

    public bool Enabled => !string.IsNullOrWhiteSpace(SecretKey);
}
