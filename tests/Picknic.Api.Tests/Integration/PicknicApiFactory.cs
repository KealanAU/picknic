using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Picknic.Api.Data;
using Picknic.Api.Email;
using Picknic.Api.Notifications;
using Picknic.Api.Storage;

namespace Picknic.Api.Tests.Integration;

public record CreatedEvent(Guid Id, string Code, string JoinSecret);
public record JoinedGuest(Guid GuestId, Guid EventId, string Token);

/// <summary>
/// Boots the real pipeline (rate limiting, sanitization, both auth schemes,
/// authorization) against SQLite in-memory and <see cref="FakeBlobSasService"/>.
/// </summary>
public class PicknicApiFactory : WebApplicationFactory<Program>
{
    public const string StripeWebhookSecret = "whsec_integration_test_secret";
    public const string EventGridSecret = "eg-integration-test-secret";

    // The in-memory database lives and dies with this connection, so it is
    // opened once and held for the factory's lifetime.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    private static int _lastIp;

    public FakeBlobSasService Blobs { get; } = new();
    public RecordingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:Guest:SigningKey", "integration-test-guest-signing-key-32+bytes");
        builder.UseSetting("Cors:AllowedOrigins", "http://localhost:3000");
        builder.UseSetting("Stripe:WebhookSecret", StripeWebhookSecret);
        builder.UseSetting("EventGrid:Secret", EventGridSecret);

        _connection.Open();
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<PicknicDbContext>));
            services.RemoveAll<DbContextOptions<PicknicDbContext>>();
            services.AddDbContext<PicknicDbContext>(o => o.UseSqlite(_connection));

            services.RemoveAll<BlobSasService>();
            services.AddSingleton<BlobSasService>(Blobs);

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            // The reveal poller would race test assertions on the shared database.
            foreach (var poller in services
                .Where(d => d.ImplementationType == typeof(RevealNotificationService)).ToList())
                services.Remove(poller);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }

    /// <summary>Client with a unique forwarded IP so per-IP rate limits don't couple tests.</summary>
    public HttpClient CreateClientWithIp(string? ip = null)
    {
        var client = CreateClient();
        var n = Interlocked.Increment(ref _lastIp);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip ?? $"10.1.{(n >> 8) & 255}.{n & 255}");
        return client;
    }

    /// <summary>Registers and logs in an Identity user, returning a client with the bearer set.</summary>
    public async Task<HttpClient> CreateHostClientAsync(string email)
    {
        const string password = "Sup3r$ecret!1";
        var client = CreateClientWithIp();

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateGuestClient(string token)
    {
        var client = CreateClientWithIp();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<CreatedEvent> CreateEventAsync(
        HttpClient host,
        DateTimeOffset? opensAt = null, DateTimeOffset? closesAt = null, DateTimeOffset? revealAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        var res = await host.PostAsJsonAsync("/api/events", new
        {
            name = "Test Roll",
            uploadOpensAt = opensAt ?? now.AddHours(-1),
            uploadClosesAt = closesAt ?? now.AddHours(4),
            revealAt = revealAt ?? now.AddHours(5),
        });
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return new CreatedEvent(
            json.GetProperty("id").GetGuid(),
            json.GetProperty("code").GetString()!,
            json.GetProperty("joinSecret").GetString()!);
    }

    public async Task<JoinedGuest> JoinAsync(
        string code, string? joinSecret = null, string name = "Guest", string? email = null)
    {
        using var client = CreateClientWithIp();
        var res = await client.PostAsJsonAsync($"/api/events/{code}/join", new { name, email, joinSecret });
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return new JoinedGuest(
            json.GetProperty("guestId").GetGuid(),
            json.GetProperty("eventId").GetGuid(),
            json.GetProperty("token").GetString()!);
    }

    public async Task<T> WithDbAsync<T>(Func<PicknicDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<PicknicDbContext>());
    }

    public Task WithDbAsync(Func<PicknicDbContext, Task> action) =>
        WithDbAsync(async db => { await action(db); return 0; });
}
