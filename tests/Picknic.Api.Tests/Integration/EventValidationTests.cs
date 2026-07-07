using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Picknic.Api.Models;

namespace Picknic.Api.Tests.Integration;

public class EventValidationTests(PicknicApiFactory factory) : IClassFixture<PicknicApiFactory>
{
    private static object EventBody(
        string name = "Valid Roll",
        DateTimeOffset? opensAt = null, DateTimeOffset? closesAt = null, DateTimeOffset? revealAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new
        {
            name,
            uploadOpensAt = opensAt ?? now.AddHours(-1),
            uploadClosesAt = closesAt ?? now.AddHours(4),
            revealAt = revealAt ?? now.AddHours(5),
        };
    }

    private static async Task AssertValidationError(HttpResponseMessage res, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var errors = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out _),
            $"Expected the '{field}' key in the validation errors, got: {errors}");
    }

    [Fact]
    public async Task Create_rejects_blank_name()
    {
        using var host = await factory.CreateHostClientAsync("create-blank-name@test.local");
        var res = await host.PostAsJsonAsync("/api/events", EventBody(name: "   "));
        await AssertValidationError(res, "name");
    }

    [Fact]
    public async Task Create_rejects_name_over_120_chars()
    {
        using var host = await factory.CreateHostClientAsync("create-long-name@test.local");

        var over = await host.PostAsJsonAsync("/api/events",
            EventBody(name: new string('x', Event.NameMaxLength + 1)));
        await AssertValidationError(over, "name");

        // The boundary itself is fine.
        var atLimit = await host.PostAsJsonAsync("/api/events",
            EventBody(name: new string('x', Event.NameMaxLength)));
        Assert.Equal(HttpStatusCode.Created, atLimit.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_upload_window_that_closes_before_it_opens()
    {
        using var host = await factory.CreateHostClientAsync("create-window@test.local");
        var now = DateTimeOffset.UtcNow;

        var res = await host.PostAsJsonAsync("/api/events",
            EventBody(opensAt: now.AddHours(2), closesAt: now.AddHours(1), revealAt: now.AddHours(3)));
        await AssertValidationError(res, "uploadOpensAt");
    }

    [Fact]
    public async Task Create_accepts_a_multi_day_window()
    {
        using var host = await factory.CreateHostClientAsync("create-multi-day@test.local");
        var now = DateTimeOffset.UtcNow;

        var res = await host.PostAsJsonAsync("/api/events",
            EventBody(opensAt: now, closesAt: now.AddDays(3), revealAt: now.AddDays(3).AddHours(1)));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_upload_window_over_the_length_cap()
    {
        using var host = await factory.CreateHostClientAsync("create-window-cap@test.local");
        var now = DateTimeOffset.UtcNow;

        var over = await host.PostAsJsonAsync("/api/events",
            EventBody(opensAt: now, closesAt: now.Add(Event.MaxUploadWindow).AddMinutes(1),
                revealAt: now.Add(Event.MaxUploadWindow).AddHours(2)));
        await AssertValidationError(over, "uploadClosesAt");

        // The boundary itself is fine.
        var atLimit = await host.PostAsJsonAsync("/api/events",
            EventBody(opensAt: now, closesAt: now.Add(Event.MaxUploadWindow),
                revealAt: now.Add(Event.MaxUploadWindow).AddHours(1)));
        Assert.Equal(HttpStatusCode.Created, atLimit.StatusCode);
    }

    [Fact]
    public async Task Join_succeeds_partway_through_a_multi_day_window()
    {
        using var host = await factory.CreateHostClientAsync("join-multi-day@test.local");
        var now = DateTimeOffset.UtcNow;

        // Day two of a three-day party.
        var ev = await factory.CreateEventAsync(host,
            opensAt: now.AddDays(-1).AddHours(-2),
            closesAt: now.AddDays(2),
            revealAt: now.AddDays(2).AddHours(1));

        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret, name: "Day Two Guest");
        Assert.NotEqual(Guid.Empty, guest.GuestId);
    }

    [Fact]
    public async Task Create_rejects_reveal_before_the_upload_window_closes()
    {
        using var host = await factory.CreateHostClientAsync("create-reveal@test.local");
        var now = DateTimeOffset.UtcNow;

        var res = await host.PostAsJsonAsync("/api/events",
            EventBody(opensAt: now, closesAt: now.AddHours(2), revealAt: now.AddHours(1)));
        await AssertValidationError(res, "revealAt");
    }

    [Fact]
    public async Task Update_rejects_blank_name()
    {
        using var host = await factory.CreateHostClientAsync("update-blank-name@test.local");
        var ev = await factory.CreateEventAsync(host);

        var res = await host.PutAsJsonAsync($"/api/events/{ev.Id}", EventBody(name: " "));
        await AssertValidationError(res, "name");
    }

    [Fact]
    public async Task Update_rejects_invalid_schedule()
    {
        using var host = await factory.CreateHostClientAsync("update-schedule@test.local");
        var ev = await factory.CreateEventAsync(host);
        var now = DateTimeOffset.UtcNow;

        var window = await host.PutAsJsonAsync($"/api/events/{ev.Id}",
            EventBody(opensAt: now.AddHours(2), closesAt: now.AddHours(2), revealAt: now.AddHours(3)));
        await AssertValidationError(window, "uploadOpensAt");

        var reveal = await host.PutAsJsonAsync($"/api/events/{ev.Id}",
            EventBody(opensAt: now, closesAt: now.AddHours(2), revealAt: now.AddHours(1)));
        await AssertValidationError(reveal, "revealAt");

        var tooLong = await host.PutAsJsonAsync($"/api/events/{ev.Id}",
            EventBody(opensAt: now, closesAt: now.Add(Event.MaxUploadWindow).AddDays(1),
                revealAt: now.Add(Event.MaxUploadWindow).AddDays(2)));
        await AssertValidationError(tooLong, "uploadClosesAt");
    }

    [Fact]
    public async Task Join_beyond_the_guest_cap_is_forbidden_and_removed_guests_free_slots()
    {
        using var host = await factory.CreateHostClientAsync("guest-cap@test.local");
        var ev = await factory.CreateEventAsync(host);
        var cap = PlanLimits.For("free").MaxGuests;

        // Fill all but one slot, plus a removed guest that must not count.
        await factory.WithDbAsync(async db =>
        {
            for (var i = 0; i < cap - 1; i++)
                db.Guests.Add(new Guest { EventId = ev.Id, DisplayName = $"Seed {i}" });
            db.Guests.Add(new Guest
            {
                EventId = ev.Id,
                DisplayName = "Kicked",
                RemovedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        // The removed guest freed a slot, so the last join succeeds...
        await factory.JoinAsync(ev.Code, ev.JoinSecret, name: "Last Slot");

        // ...and the event is now at cap.
        using var client = factory.CreateClientWithIp();
        var over = await client.PostAsJsonAsync($"/api/events/{ev.Code}/join",
            new { name = "Overflow", joinSecret = ev.JoinSecret });
        Assert.Equal(HttpStatusCode.Forbidden, over.StatusCode);
        var problem = await over.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("guest limit", problem.GetProperty("detail").GetString());
    }
}
