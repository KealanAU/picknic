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
        .WithName("CreateCheckoutSession");

        return app;
    }
}
