using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Picknic.Api.Data;
using Picknic.Api.Models;
using Stripe;
using Stripe.Checkout;

namespace Picknic.Api.Payments;

public record CheckoutRequest(string EventCode, string Plan);

public static class CheckoutEndpoints
{
    // Plan -> price in the smallest currency unit (pence). Replace with real
    // Stripe Price IDs once products are set up in the Stripe dashboard.
    private static readonly Dictionary<string, (string Name, long Amount)> Plans = new()
    {
        ["premium"] = ("Picknic Premium Event", 1500),
        ["pro"] = ("Picknic Pro Event", 3500),
    };

    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        // The host starts an upgrade for an event they own. Payment methods are
        // left to Stripe's automatic selection, so Apple Pay / Google Pay / cards
        // appear on the hosted Checkout page per the account's dashboard settings.
        app.MapPost("/api/checkout", async (
            CheckoutRequest req, ClaimsPrincipal user,
            IOptions<StripeOptions> opts, PicknicDbContext db) =>
        {
            var stripe = opts.Value;
            if (!stripe.Enabled)
                return Results.Problem("Payments are not configured.", statusCode: 501);

            if (!Plans.TryGetValue(req.Plan, out var plan))
                return Results.Problem($"Unknown plan '{req.Plan}'.", statusCode: 400);

            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var code = req.EventCode.ToUpperInvariant();
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Code == code);
            if (ev is null) return Results.NotFound();
            if (ev.HostId != hostId) return Results.Forbid();

            StripeConfiguration.ApiKey = stripe.SecretKey;

            // Prefer a real dashboard Price ID when configured; otherwise fall back
            // to an ad-hoc inline price so the flow works before products are set up.
            var lineItem = stripe.Prices.TryGetValue(req.Plan, out var priceId)
                && !string.IsNullOrWhiteSpace(priceId)
                ? new SessionLineItemOptions { Quantity = 1, Price = priceId }
                : new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "gbp",
                        UnitAmount = plan.Amount,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = plan.Name },
                    },
                };

            var session = await new SessionService().CreateAsync(new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = stripe.SuccessUrl,
                CancelUrl = stripe.CancelUrl,
                ClientReferenceId = ev.Code,
                Metadata = new() { ["plan"] = req.Plan, ["eventCode"] = ev.Code },
                LineItems = [lineItem],
            });

            return Results.Ok(new { id = session.Id, url = session.Url });
        })
        .RequireAuthorization("Host")
        .RequireRateLimiting("checkout")
        .WithName("CreateCheckoutSession");

        app.MapPost("/api/checkout/webhook", async (HttpRequest request, IOptions<StripeOptions> opts, Data.PicknicDbContext db) =>
        {
            var stripe = opts.Value;
            if (string.IsNullOrEmpty(stripe.WebhookSecret))
                return Results.Problem("Webhook is not configured.", statusCode: 501);

            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();

            Stripe.Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], stripe.WebhookSecret);
            }
            catch (StripeException)
            {
                return Results.Problem("Invalid Stripe webhook signature.", statusCode: 400);
            }

            // Stripe delivers at-least-once; skip anything already handled.
            if (await db.ProcessedStripeEvents.AnyAsync(e => e.Id == stripeEvent.Id))
                return Results.Ok();

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session is not null && session.PaymentStatus == "paid")
                {
                    var plan = session.Metadata?.GetValueOrDefault("plan");
                    var eventCode = session.ClientReferenceId
                        ?? session.Metadata?.GetValueOrDefault("eventCode");

                    if (!string.IsNullOrEmpty(plan) && !string.IsNullOrEmpty(eventCode))
                    {
                        var code = eventCode.ToUpperInvariant();
                        var ev = await db.Events.FirstOrDefaultAsync(e => e.Code == code);
                        if (ev is not null)
                        {
                            ev.Tier = plan;
                            ev.PaidAt = DateTimeOffset.UtcNow;
                        }
                    }
                }
            }

            // Record the event as handled in the same transaction as any fulfillment.
            db.ProcessedStripeEvents.Add(new ProcessedStripeEvent { Id = stripeEvent.Id });
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // A concurrent delivery already recorded it — treat as success.
            }

            return Results.Ok();
        })
        .WithName("StripeWebhook");

        return app;
    }
}
