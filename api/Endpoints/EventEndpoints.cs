using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Auth;
using Picknic.Api.Data;
using Picknic.Api.Models;
using QRCoder;

namespace Picknic.Api.Endpoints;

public record CreateEventRequest(
    string Name,
    DateTimeOffset UploadOpensAt,
    DateTimeOffset UploadClosesAt,
    DateTimeOffset RevealAt);

public record JoinRequest(string JoinSecret, string Name, string? Email);

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        // The join secret is returned once here and only ever stored encrypted.
        group.MapPost("/", async (
            CreateEventRequest req, ClaimsPrincipal user,
            PicknicDbContext db, JoinSecretProtector secrets) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (hostId is null) return Results.Unauthorized();

            var secret = GuestTokenService.NewSecret();
            var ev = new Event
            {
                Code = NewCode(),
                Name = req.Name,
                JoinSecretEnc = secrets.Protect(secret),
                UploadOpensAt = req.UploadOpensAt,
                UploadClosesAt = req.UploadClosesAt,
                RevealAt = req.RevealAt,
                HostId = hostId,
            };
            db.Events.Add(ev);
            await db.SaveChangesAsync();

            return Results.Created($"/api/events/{ev.Code}", new
            {
                ev.Id,
                ev.Code,
                joinSecret = secret, // goes in the QR
                ev.UploadOpensAt,
                ev.UploadClosesAt,
                ev.RevealAt,
            });
        })
        .RequireAuthorization("Host")
        .WithName("CreateEvent");

        group.MapGet("/{code}", async (string code, PicknicDbContext db) =>
        {
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Code == code.ToUpperInvariant());
            if (ev is null) return Results.NotFound();
            var now = DateTimeOffset.UtcNow;
            return Results.Ok(new
            {
                ev.Code,
                ev.Name,
                uploadOpen = ev.UploadOpen(now),
                revealed = ev.Revealed(now),
                ev.UploadClosesAt,
                ev.RevealAt,
            });
        })
        .WithName("GetEvent");

        // Host fetches a printable QR + join link at any time.
        group.MapGet("/{id:guid}/qr", async (
            Guid id, ClaimsPrincipal user,
            PicknicDbContext db, JoinSecretProtector secrets, EventLinks links) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();
            if (ev.HostId != hostId) return Results.Forbid();

            var joinUrl = links.JoinUrl(ev.Code, secrets.Unprotect(ev.JoinSecretEnc));

            using var qr = new QRCodeGenerator();
            var data = qr.CreateQrCode(joinUrl, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data).GetGraphic(10);

            return Results.Ok(new
            {
                ev.Code,
                joinUrl,
                qrPng = $"data:image/png;base64,{Convert.ToBase64String(png)}",
            });
        })
        .RequireAuthorization("Host")
        .WithName("GetEventQr");

        // Guest joins with the QR secret and their name -> scoped token + guest UUID.
        group.MapPost("/{code}/join", async (
            string code, JoinRequest req, PicknicDbContext db,
            JoinSecretProtector secrets, GuestTokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest("A name is required.");

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Code == code.ToUpperInvariant());
            if (ev is null) return Results.NotFound();

            if (!SecretMatches(req.JoinSecret, secrets.Unprotect(ev.JoinSecretEnc)))
                return Results.Unauthorized();

            var now = DateTimeOffset.UtcNow;
            if (!ev.UploadOpen(now))
                return Results.Problem("Uploads are closed for this event.", statusCode: 403);

            var guest = new Guest
            {
                EventId = ev.Id,
                DisplayName = req.Name.Trim(),
                Email = req.Email,
            };
            db.Guests.Add(guest);

            var invite = req.Email is null ? null
                : await db.Invites.FirstOrDefaultAsync(i => i.EventId == ev.Id && i.Email == req.Email);
            if (invite is not null) invite.AcceptedAt = now;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                guestId = guest.Id,
                name = guest.DisplayName,
                token = tokens.Issue(ev.Id, guest.Id, ev.UploadClosesAt),
                expiresAt = ev.UploadClosesAt,
                eventId = ev.Id,
            });
        })
        .WithName("JoinEvent");

        return app;
    }

    private static bool SecretMatches(string provided, string actual) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(actual));

    private static string NewCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Concat(Enumerable.Range(0, 6)
            .Select(_ => alphabet[Random.Shared.Next(alphabet.Length)]));
    }
}
