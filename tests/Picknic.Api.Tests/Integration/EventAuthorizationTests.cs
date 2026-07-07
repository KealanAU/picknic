using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Picknic.Api.Tests.Integration;

public class EventAuthorizationTests(PicknicApiFactory factory) : IClassFixture<PicknicApiFactory>
{
    private static object EventBody(string name = "x")
    {
        var now = DateTimeOffset.UtcNow;
        return new
        {
            name,
            uploadOpensAt = now,
            uploadClosesAt = now.AddHours(1),
            revealAt = now.AddHours(2),
        };
    }

    [Fact]
    public async Task Host_routes_reject_anonymous_callers()
    {
        using var client = factory.CreateClientWithIp();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/events", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/events")).StatusCode);
    }

    [Fact]
    public async Task Guest_token_is_rejected_on_host_routes()
    {
        using var host = await factory.CreateHostClientAsync("guest-vs-host@test.local");
        var ev = await factory.CreateEventAsync(host);
        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret);

        using var guestClient = factory.CreateGuestClient(guest.Token);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await guestClient.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await guestClient.GetAsync($"/api/events/{ev.Id}/qr")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await guestClient.GetAsync($"/api/events/{ev.Id}/guests")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await guestClient.DeleteAsync($"/api/events/{ev.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await guestClient.DeleteAsync($"/api/events/{ev.Id}/guests/{guest.GuestId}")).StatusCode);
    }

    [Fact]
    public async Task Host_cannot_access_another_hosts_event()
    {
        using var owner = await factory.CreateHostClientAsync("owner@test.local");
        using var intruder = await factory.CreateHostClientAsync("intruder@test.local");
        var ev = await factory.CreateEventAsync(owner);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await intruder.GetAsync($"/api/events/{ev.Id}/qr")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await intruder.PutAsJsonAsync($"/api/events/{ev.Id}", EventBody("hijacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await intruder.DeleteAsync($"/api/events/{ev.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await intruder.GetAsync($"/api/events/{ev.Id}/guests")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await intruder.PostAsJsonAsync($"/api/events/{ev.Id}/invites",
                new { emails = new[] { "someone@test.local" } })).StatusCode);

        // The event survives untouched for its owner.
        Assert.Equal(HttpStatusCode.OK,
            (await owner.GetAsync($"/api/events/{ev.Id}/qr")).StatusCode);
    }

    [Fact]
    public async Task ListEvents_returns_only_the_callers_events()
    {
        using var hostA = await factory.CreateHostClientAsync("lists-a@test.local");
        using var hostB = await factory.CreateHostClientAsync("lists-b@test.local");
        var evA = await factory.CreateEventAsync(hostA);
        await factory.CreateEventAsync(hostB);

        var events = await hostA.GetFromJsonAsync<JsonElement>("/api/events");
        var only = Assert.Single(events.EnumerateArray());
        Assert.Equal(evA.Code, only.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Unknown_event_id_is_not_found()
    {
        using var host = await factory.CreateHostClientAsync("unknown-id@test.local");
        Assert.Equal(HttpStatusCode.NotFound,
            (await host.GetAsync($"/api/events/{Guid.NewGuid()}/qr")).StatusCode);
    }
}
