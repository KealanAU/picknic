using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Picknic.Api.Tests.Integration;

public class WebhookTests(PicknicApiFactory factory) : IClassFixture<PicknicApiFactory>
{
    // ---- Stripe checkout webhook ----

    private static string CheckoutCompletedPayload(string stripeEventId, string eventCode, string plan) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["id"] = stripeEventId,
            ["object"] = "event",
            // Must match the SDK's pinned version or ConstructEvent rejects the event.
            ["api_version"] = StripeConfiguration.ApiVersion,
            ["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["livemode"] = false,
            ["type"] = "checkout.session.completed",
            ["data"] = new Dictionary<string, object?>
            {
                ["object"] = new Dictionary<string, object?>
                {
                    ["id"] = "cs_test_1",
                    ["object"] = "checkout.session",
                    ["payment_status"] = "paid",
                    ["client_reference_id"] = eventCode,
                    ["metadata"] = new Dictionary<string, string>
                    {
                        ["plan"] = plan,
                        ["eventCode"] = eventCode,
                    },
                },
            },
        });

    private static HttpRequestMessage SignedWebhookRequest(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(PicknicApiFactory.StripeWebhookSecret),
            Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");
        return request;
    }

    [Fact]
    public async Task Stripe_webhook_rejects_bad_signatures()
    {
        using var host = await factory.CreateHostClientAsync("stripe-sig@test.local");
        var ev = await factory.CreateEventAsync(host);
        var payload = CheckoutCompletedPayload($"evt_{Guid.NewGuid():n}", ev.Code, "premium");

        using var client = factory.CreateClientWithIp();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", "t=12345,v1=deadbeef");

        var res = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("free", await factory.WithDbAsync(db =>
            db.Events.Where(e => e.Id == ev.Id).Select(e => e.Tier).SingleAsync()));
    }

    [Fact]
    public async Task Stripe_webhook_upgrades_the_event_and_is_idempotent()
    {
        using var host = await factory.CreateHostClientAsync("stripe-upgrade@test.local");
        var ev = await factory.CreateEventAsync(host);
        var payload = CheckoutCompletedPayload($"evt_{Guid.NewGuid():n}", ev.Code, "premium");

        using var client = factory.CreateClientWithIp();
        var first = await client.SendAsync(SignedWebhookRequest(payload));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var upgraded = await factory.WithDbAsync(db => db.Events.SingleAsync(e => e.Id == ev.Id));
        Assert.Equal("premium", upgraded.Tier);
        Assert.NotNull(upgraded.PaidAt);

        // Undo the fulfillment, then replay the same delivery: Stripe sends
        // at-least-once, and a replay must be skipped as already processed.
        await factory.WithDbAsync(async db =>
        {
            var tracked = await db.Events.SingleAsync(e => e.Id == ev.Id);
            tracked.Tier = "free";
            await db.SaveChangesAsync();
        });

        var replay = await client.SendAsync(SignedWebhookRequest(payload));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal("free", await factory.WithDbAsync(db =>
            db.Events.Where(e => e.Id == ev.Id).Select(e => e.Tier).SingleAsync()));
    }

    // ---- Event Grid blob-created webhook ----

    private static string BlobCreatedPayload(Guid eventId, Guid guestId, string fileName) =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid().ToString(),
                subject = $"/blobServices/default/containers/photos/blobs/{eventId}/{guestId}/{fileName}",
                eventType = "Microsoft.Storage.BlobCreated",
                eventTime = DateTimeOffset.UtcNow,
                data = new
                {
                    api = "PutBlob",
                    contentType = "image/jpeg",
                    contentLength = 1024,
                    blobType = "BlockBlob",
                    url = $"https://blobs.invalid/photos/{eventId}/{guestId}/{fileName}",
                },
                dataVersion = "1.0",
            },
        });

    private const string EventGridUrl = "/api/uploads/events?code=" + PicknicApiFactory.EventGridSecret;

    [Fact]
    public async Task EventGrid_webhook_requires_the_shared_secret()
    {
        using var client = factory.CreateClientWithIp();
        var payload = BlobCreatedPayload(Guid.NewGuid(), Guid.NewGuid(), "x.jpg");

        var missing = await client.PostAsync("/api/uploads/events",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);

        var wrong = await client.PostAsync("/api/uploads/events?code=nope",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task EventGrid_blob_created_registers_the_photo()
    {
        using var host = await factory.CreateHostClientAsync("eventgrid-ok@test.local");
        var ev = await factory.CreateEventAsync(host);
        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret);

        var blobPath = $"{ev.Id}/{guest.GuestId}/landed.jpg";
        factory.Blobs.Land(blobPath);

        using var client = factory.CreateClientWithIp();
        var res = await client.PostAsync(EventGridUrl,
            new StringContent(BlobCreatedPayload(ev.Id, guest.GuestId, "landed.jpg"),
                Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Assert.Equal(1, await factory.WithDbAsync(db =>
            db.Photos.CountAsync(p => p.BlobPath == blobPath)));
    }

    [Fact]
    public async Task EventGrid_blob_created_for_a_kicked_guest_drops_the_blob()
    {
        using var host = await factory.CreateHostClientAsync("eventgrid-kicked@test.local");
        var ev = await factory.CreateEventAsync(host);
        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret);
        (await host.DeleteAsync($"/api/events/{ev.Id}/guests/{guest.GuestId}")).EnsureSuccessStatusCode();

        var blobPath = $"{ev.Id}/{guest.GuestId}/orphan.jpg";
        factory.Blobs.Land(blobPath);

        using var client = factory.CreateClientWithIp();
        var res = await client.PostAsync(EventGridUrl,
            new StringContent(BlobCreatedPayload(ev.Id, guest.GuestId, "orphan.jpg"),
                Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Assert.False(await factory.Blobs.ExistsAsync(blobPath));
        Assert.Equal(0, await factory.WithDbAsync(db =>
            db.Photos.CountAsync(p => p.BlobPath == blobPath)));
    }

    [Fact]
    public async Task EventGrid_subscription_validation_echoes_the_code()
    {
        var payload = JsonSerializer.Serialize(new[]
        {
            new
            {
                id = Guid.NewGuid().ToString(),
                subject = "",
                eventType = "Microsoft.EventGrid.SubscriptionValidationEvent",
                eventTime = DateTimeOffset.UtcNow,
                data = new { validationCode = "handshake-123" },
                dataVersion = "1.0",
            },
        });

        using var client = factory.CreateClientWithIp();
        var res = await client.PostAsync(EventGridUrl,
            new StringContent(payload, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("handshake-123", body);
    }
}
