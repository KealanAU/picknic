namespace Picknic.Api.Models;

/// <summary>
/// A Stripe webhook event we've already handled. Stripe delivers events
/// at-least-once, so the webhook records each event id here and skips any it has
/// seen — making fulfillment idempotent.
/// </summary>
public class ProcessedStripeEvent
{
    /// <summary>The Stripe event id (evt_...).</summary>
    public required string Id { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
