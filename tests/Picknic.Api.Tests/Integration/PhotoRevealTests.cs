using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Picknic.Api.FilmProcessing;
using Picknic.Api.Models;

namespace Picknic.Api.Tests.Integration;

public class PhotoRevealTests(PicknicApiFactory factory) : IClassFixture<PicknicApiFactory>
{
    [Fact]
    public async Task Photos_are_hidden_before_reveal()
    {
        using var host = await factory.CreateHostClientAsync("reveal-hidden@test.local");
        var ev = await factory.CreateEventAsync(host);
        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret);
        await SeedPhotoAsync(ev.Id, guest.GuestId);

        using var client = factory.CreateClientWithIp();
        var res = await client.GetAsync($"/api/events/{ev.Id}/photos");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("revealed").GetBoolean());
        Assert.False(json.TryGetProperty("photos", out _));
    }

    [Fact]
    public async Task Revealed_photos_serve_the_developed_derivative_when_it_exists()
    {
        using var host = await factory.CreateHostClientAsync("reveal-urls@test.local");
        var ev = await factory.CreateEventAsync(host);
        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret, name: "Snapper");

        var original = await SeedPhotoAsync(ev.Id, guest.GuestId);
        var developed = await SeedPhotoAsync(ev.Id, guest.GuestId, developed: true);
        await factory.WithDbAsync(async db =>
        {
            (await db.Events.FindAsync(ev.Id))!.RevealAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });

        using var client = factory.CreateClientWithIp();
        var json = await client.GetFromJsonAsync<JsonElement>($"/api/events/{ev.Id}/photos");
        Assert.True(json.GetProperty("revealed").GetBoolean());

        var urls = json.GetProperty("photos").EnumerateArray()
            .ToDictionary(p => p.GetProperty("id").GetGuid(), p => p.GetProperty("url").GetString()!);
        Assert.Equal(2, urls.Count);

        // The undeveloped photo signs its original blob; the developed one signs
        // the derivative FilmDeveloper writes alongside it.
        Assert.Contains(original.BlobPath, urls[original.Id]);
        Assert.DoesNotContain(".dev.jpg", urls[original.Id]);
        Assert.Contains(FilmDeveloper.DevelopedPath(developed.BlobPath), urls[developed.Id]);
    }

    [Fact]
    public async Task Develop_rejects_an_unknown_film_stock()
    {
        using var host = await factory.CreateHostClientAsync("develop-stock@test.local");
        var ev = await factory.CreateEventAsync(host);
        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret);
        var photo = await SeedPhotoAsync(ev.Id, guest.GuestId);

        var res = await host.PostAsync(
            $"/api/events/{ev.Id}/photos/{photo.Id}/develop?stock=not-a-stock", null);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var errors = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("stock", out _));
    }

    [Fact]
    public async Task Develop_rejects_an_unknown_print_style()
    {
        using var host = await factory.CreateHostClientAsync("develop-print@test.local");
        var ev = await factory.CreateEventAsync(host);
        var guest = await factory.JoinAsync(ev.Code, ev.JoinSecret);
        var photo = await SeedPhotoAsync(ev.Id, guest.GuestId);

        var res = await host.PostAsync(
            $"/api/events/{ev.Id}/photos/{photo.Id}/develop?print=not-a-print", null);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var errors = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("print", out _));
    }

    private Task<Photo> SeedPhotoAsync(Guid eventId, Guid guestId, bool developed = false) =>
        factory.WithDbAsync(async db =>
        {
            var photo = new Photo
            {
                EventId = eventId,
                UploadedByGuestId = guestId,
                BlobPath = $"{eventId}/{guestId}/{Guid.NewGuid():n}.jpg",
                SizeBytes = 1024,
                DevelopedAt = developed ? DateTimeOffset.UtcNow : null,
            };
            db.Photos.Add(photo);
            await db.SaveChangesAsync();
            return photo;
        });
}
