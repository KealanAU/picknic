using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Models;

namespace Picknic.Api.Tests.Integration;

public class UploadFlowTests(PicknicApiFactory factory) : IClassFixture<PicknicApiFactory>
{
    private async Task<(CreatedEvent Event, JoinedGuest Guest, HttpClient GuestClient, HttpClient HostClient)>
        SetupAsync(string hostEmail)
    {
        var host = await factory.CreateHostClientAsync(hostEmail);
        var ev = await factory.CreateEventAsync(host);
        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret);
        return (ev, guest, factory.CreateGuestClient(guest.Token), host);
    }

    /// <summary>Requests a SAS, lands the blob, and calls the complete callback.</summary>
    private async Task<(HttpResponseMessage Complete, string BlobPath)> UploadPhotoAsync(
        HttpClient guestClient, CreatedEvent ev,
        long size = 1024, string? contentType = "image/jpeg", string? caption = null)
    {
        var sas = await guestClient.PostAsync($"/api/events/{ev.Id}/uploads", null);
        sas.EnsureSuccessStatusCode();
        var blobPath = (await sas.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("blobPath").GetString()!;

        factory.Blobs.Land(blobPath, size, contentType);
        var complete = await guestClient.PostAsJsonAsync(
            $"/api/events/{ev.Id}/uploads/complete", new { blobPath, caption });
        return (complete, blobPath);
    }

    [Fact]
    public async Task Join_requires_matching_secret()
    {
        var (ev, _, _, _) = await SetupAsync("join-secret@test.local");

        using var client = factory.CreateClientWithIp();
        var res = await client.PostAsJsonAsync($"/api/events/{ev.Code}/join",
            new { name = "Mallory", joinSecret = "wrong-secret" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Join_rejects_blank_name()
    {
        var (ev, _, _, _) = await SetupAsync("join-name@test.local");

        using var client = factory.CreateClientWithIp();
        var res = await client.PostAsJsonAsync($"/api/events/{ev.Code}/join",
            new { name = "  ", joinSecret = ev.JoinSecret });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Join_is_rejected_outside_the_upload_window()
    {
        using var host = await factory.CreateHostClientAsync("join-window@test.local");
        var now = DateTimeOffset.UtcNow;
        var ev = await factory.CreateEventAsync(host,
            opensAt: now.AddHours(1), closesAt: now.AddHours(2), revealAt: now.AddHours(3));

        using var client = factory.CreateClientWithIp();
        var res = await client.PostAsJsonAsync($"/api/events/{ev.Code}/join",
            new { name = "Early Bird", joinSecret = ev.JoinSecret });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Join_is_rate_limited_per_ip()
    {
        var (ev, _, _, _) = await SetupAsync("join-ratelimit@test.local");

        using var client = factory.CreateClientWithIp("10.99.99.99");
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++)
        {
            var res = await client.PostAsJsonAsync($"/api/events/{ev.Code}/join",
                new { name = $"Guest {i}", joinSecret = ev.JoinSecret });
            statuses.Add(res.StatusCode);
        }

        Assert.Equal(10, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }

    [Fact]
    public async Task Guest_gets_an_upload_target_scoped_to_their_folder()
    {
        var (ev, guest, guestClient, _) = await SetupAsync("upload-sas@test.local");

        var res = await guestClient.PostAsync($"/api/events/{ev.Id}/uploads", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var blobPath = (await res.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("blobPath").GetString();
        Assert.StartsWith($"{ev.Id}/{guest.GuestId}/", blobPath);
    }

    [Fact]
    public async Task Guest_token_does_not_work_on_another_event()
    {
        var (_, _, guestClient, _) = await SetupAsync("cross-event-a@test.local");
        using var otherHost = await factory.CreateHostClientAsync("cross-event-b@test.local");
        var otherEvent = await factory.CreateEventAsync(otherHost);

        var res = await guestClient.PostAsync($"/api/events/{otherEvent.Id}/uploads", null);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        var complete = await guestClient.PostAsJsonAsync(
            $"/api/events/{otherEvent.Id}/uploads/complete",
            new { blobPath = $"{otherEvent.Id}/{Guid.NewGuid()}/x.jpg", caption = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, complete.StatusCode);
    }

    [Fact]
    public async Task Host_token_is_rejected_on_guest_routes()
    {
        var (ev, _, _, host) = await SetupAsync("host-vs-guest@test.local");

        var res = await host.PostAsync($"/api/events/{ev.Id}/uploads", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Kicked_guest_token_is_rejected()
    {
        var (ev, guest, guestClient, host) = await SetupAsync("kick-token@test.local");

        var kick = await host.DeleteAsync($"/api/events/{ev.Id}/guests/{guest.GuestId}");
        Assert.Equal(HttpStatusCode.OK, kick.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await guestClient.PostAsync($"/api/events/{ev.Id}/uploads", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await guestClient.PostAsJsonAsync($"/api/events/{ev.Id}/uploads/complete",
                new { blobPath = $"{ev.Id}/{guest.GuestId}/x.jpg", caption = (string?)null })).StatusCode);
    }

    [Fact]
    public async Task Kicking_a_guest_deletes_their_photos_and_blobs()
    {
        var (ev, guest, guestClient, host) = await SetupAsync("kick-photos@test.local");
        var (complete, blobPath) = await UploadPhotoAsync(guestClient, ev);
        complete.EnsureSuccessStatusCode();

        var kick = await host.DeleteAsync($"/api/events/{ev.Id}/guests/{guest.GuestId}");
        Assert.Equal(HttpStatusCode.OK, kick.StatusCode);

        Assert.False(await factory.Blobs.ExistsAsync(blobPath));
        Assert.Equal(0, await factory.WithDbAsync(db =>
            db.Photos.CountAsync(p => p.UploadedByGuestId == guest.GuestId)));
    }

    [Fact]
    public async Task Complete_rejects_blob_paths_outside_the_guests_folder()
    {
        var (ev, _, guestClient, _) = await SetupAsync("foreign-path@test.local");
        var foreignPath = $"{ev.Id}/{Guid.NewGuid()}/stolen.jpg";
        factory.Blobs.Land(foreignPath);

        var res = await guestClient.PostAsJsonAsync(
            $"/api/events/{ev.Id}/uploads/complete", new { blobPath = foreignPath, caption = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Complete_is_idempotent_per_blob()
    {
        var (ev, guest, guestClient, _) = await SetupAsync("idempotent@test.local");
        var (first, blobPath) = await UploadPhotoAsync(guestClient, ev);
        first.EnsureSuccessStatusCode();
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        // A second delivery for the same blob (with a late caption) must not
        // duplicate the photo — it fills in the blank caption instead.
        var second = await guestClient.PostAsJsonAsync(
            $"/api/events/{ev.Id}/uploads/complete", new { blobPath, caption = "late caption" });
        second.EnsureSuccessStatusCode();
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetGuid();

        Assert.Equal(firstId, secondId);
        var photos = await factory.WithDbAsync(db =>
            db.Photos.Where(p => p.UploadedByGuestId == guest.GuestId).ToListAsync());
        var photo = Assert.Single(photos);
        Assert.Equal("late caption", photo.Caption);
    }

    [Fact]
    public async Task Photo_cap_is_enforced_per_guest()
    {
        var (ev, guest, guestClient, _) = await SetupAsync("photo-cap@test.local");
        var cap = PlanLimits.For("free").MaxPhotosPerGuest;

        // Seed all but one photo directly; the last slot and the overflow go
        // through the real endpoint.
        await factory.WithDbAsync(async db =>
        {
            for (var i = 0; i < cap - 1; i++)
                db.Photos.Add(new Photo
                {
                    EventId = ev.Id,
                    UploadedByGuestId = guest.GuestId,
                    BlobPath = $"{ev.Id}/{guest.GuestId}/seed{i}.jpg",
                    SizeBytes = 100,
                });
            await db.SaveChangesAsync();
        });

        var (atCap, _) = await UploadPhotoAsync(guestClient, ev);
        Assert.Equal(HttpStatusCode.OK, atCap.StatusCode);

        var (overCap, overCapPath) = await UploadPhotoAsync(guestClient, ev);
        Assert.Equal(HttpStatusCode.Forbidden, overCap.StatusCode);
        // The rejected blob must not linger in storage.
        Assert.False(await factory.Blobs.ExistsAsync(overCapPath));
        Assert.Equal(cap, await factory.WithDbAsync(db =>
            db.Photos.CountAsync(p => p.UploadedByGuestId == guest.GuestId)));
    }

    [Fact]
    public async Task Oversized_blobs_are_rejected_and_deleted()
    {
        var (ev, _, guestClient, _) = await SetupAsync("oversize@test.local");

        var (res, blobPath) = await UploadPhotoAsync(guestClient, ev, size: factory.Blobs.MaxBytes + 1);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.False(await factory.Blobs.ExistsAsync(blobPath));
    }

    [Fact]
    public async Task Non_image_blobs_are_rejected_and_deleted()
    {
        var (ev, _, guestClient, _) = await SetupAsync("non-image@test.local");

        var (res, blobPath) = await UploadPhotoAsync(guestClient, ev, contentType: "application/pdf");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.False(await factory.Blobs.ExistsAsync(blobPath));
    }
}
