using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        app.MapPost("/api/checkout", (CheckoutRequest req, IOptions<StripeOptions> opts) =>
        {
            var stripe = opts.Value;
            if (!stripe.Enabled)
                return Results.Problem("Payments are not configured.", statusCode: 501);

            if (!Plans.TryGetValue(req.Plan, out var plan))
                return Results.BadRequest(new { error = $"Unknown plan '{req.Plan}'." });

            StripeConfiguration.ApiKey = stripe.SecretKey;

            var session = new SessionService().Create(new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = stripe.SuccessUrl,
                CancelUrl = stripe.CancelUrl,
                ClientReferenceId = req.EventCode,
                Metadata = new() { ["plan"] = req.Plan, ["eventCode"] = req.EventCode },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "gbp",
                            UnitAmount = plan.Amount,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = plan.Name,
                            },
                        },
                    },
                ],
            });

            return Results.Ok(new { id = session.Id, url = session.Url });
        })
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
                return Results.BadRequest();
            }

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
                            await db.SaveChangesAsync();
                        }
                    }
                }
            }

            return Results.Ok();
        })
        .WithName("StripeWebhook");

        return app;
    }
}
