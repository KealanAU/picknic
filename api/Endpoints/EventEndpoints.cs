using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Auth;
using Picknic.Api.Data;
using Picknic.Api.Models;
using Picknic.Api.Storage;
using QRCoder;

namespace Picknic.Api.Endpoints;

public record CreateEventRequest(
    string Name,
    DateTimeOffset UploadOpensAt,
    DateTimeOffset UploadClosesAt,
    DateTimeOffset RevealAt);

public record UpdateEventRequest(
    string Name,
    DateTimeOffset UploadOpensAt,
    DateTimeOffset UploadClosesAt,
    DateTimeOffset RevealAt);

public record JoinRequest(string Name, string? Email, string? JoinSecret);

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

            if (ValidateEventInput(req.Name, req.UploadOpensAt, req.UploadClosesAt, req.RevealAt) is { } invalid)
                return invalid;

            var secret = GuestTokenService.NewSecret();
            var ev = new Event
            {
                Code = NewCode(),
                Name = req.Name.Trim(),
                JoinSecretEnc = secrets.Protect(secret),
                UploadOpensAt = req.UploadOpensAt,
                UploadClosesAt = req.UploadClosesAt,
                RevealAt = req.RevealAt,
                HostId = hostId,
            };
            db.Events.Add(ev);
            // The 6-char code has a unique index; on the rare collision mint a new
            // one and retry instead of surfacing a 500.
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await db.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException) when (attempt < 4)
                {
                    ev.Code = NewCode();
                }
            }

            return Results.Created($"/api/events/{ev.Code}", new
            {
                ev.Id,
                ev.Code,
                joinSecret = secret,
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

        group.MapGet("/{id:guid}/qr", async (
            Guid id, ClaimsPrincipal user,
            PicknicDbContext db, JoinSecretProtector secrets, EventLinks links) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();
            if (ev.HostId != hostId) return Results.Forbid();

            var joinSecret = secrets.UnprotectOrRotate(ev, out var rotated);
            if (rotated) await db.SaveChangesAsync();

            var joinUrl = links.JoinUrl(ev.Code, joinSecret);

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

        // Two entry paths for joining: a scanned QR carries the high-entropy join secret in
        // its fragment, while a typed room code carries none. When a secret is
        // supplied it must match; when it's absent the room code plus the open
        // upload window (both enforced below) are the gate.
        group.MapPost("/{code}/join", async (
            string code, JoinRequest req, PicknicDbContext db,
            JoinSecretProtector secrets, GuestTokenService tokens) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(req.Name))
                errors["name"] = ["A name is required."];
            else if (req.Name.Trim().Length > Guest.DisplayNameMaxLength)
                errors["name"] = [$"Name must be {Guest.DisplayNameMaxLength} characters or fewer."];
            if (req.Email is { Length: > Invite.EmailMaxLength })
                errors["email"] = [$"Email must be {Invite.EmailMaxLength} characters or fewer."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Code == code.ToUpperInvariant());
            if (ev is null) return Results.NotFound();

            if (!string.IsNullOrEmpty(req.JoinSecret)
                && (!secrets.TryUnprotect(ev.JoinSecretEnc, out var joinSecret)
                    || !SecretMatches(req.JoinSecret, joinSecret)))
                return Results.Unauthorized();

            var now = DateTimeOffset.UtcNow;
            if (!ev.UploadOpen(now))
                return Results.Problem("Uploads are closed for this event.", statusCode: 403);

            var cap = PlanLimits.For(ev.Tier).MaxGuests;
            var guestCount = await db.Guests.CountAsync(
                g => g.EventId == ev.Id && g.RemovedAt == null && !g.IsHost);
            if (guestCount >= cap)
                return Results.Problem("This event has reached its guest limit.", statusCode: 403);

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

            return Results.Created((string?)null, new
            {
                guestId = guest.Id,
                name = guest.DisplayName,
                token = tokens.Issue(ev.Id, guest.Id, ev.UploadClosesAt),
                expiresAt = ev.UploadClosesAt,
                eventId = ev.Id,
            });
        })
        .RequireRateLimiting("join")
        .WithName("JoinEvent");

        // Mints the host an upload identity on their own event: a Guest row
        // flagged IsHost plus the same event-scoped token guests get, so the
        // host shoots through the untouched upload pipeline (SAS mint, blob
        // path pinning, photo caps). Idempotent — reuses and restores the row.
        group.MapPost("/{id:guid}/camera-pass", async (
            Guid id, ClaimsPrincipal user, PicknicDbContext db, GuestTokenService tokens) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();
            if (ev.HostId != hostId) return Results.Forbid();

            var now = DateTimeOffset.UtcNow;
            if (!ev.UploadOpen(now))
                return Results.Problem("Uploads are closed for this party.", statusCode: 403);

            var guest = await db.Guests.FirstOrDefaultAsync(g => g.EventId == id && g.IsHost);
            if (guest is null)
            {
                guest = new Guest { EventId = id, DisplayName = "Host", IsHost = true };
                db.Guests.Add(guest);
            }
            guest.RemovedAt = null;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                guestId = guest.Id,
                name = guest.DisplayName,
                token = tokens.Issue(id, guest.Id, ev.UploadClosesAt),
                expiresAt = ev.UploadClosesAt,
                eventId = id,
            });
        })
        .RequireAuthorization("Host")
        .WithName("CreateCameraPass");

        group.MapGet("/", async (ClaimsPrincipal user, PicknicDbContext db) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (hostId is null) return Results.Unauthorized();

            var now = DateTimeOffset.UtcNow;
            var events = await db.Events
                .Where(e => e.HostId == hostId)
                .OrderByDescending(e => e.UploadOpensAt)
                .ToListAsync();

            // Two grouped aggregates instead of a count pair per event (N+1).
            var eventIds = events.Select(e => e.Id).ToList();
            var guestCounts = await db.Guests
                .Where(g => eventIds.Contains(g.EventId) && g.RemovedAt == null)
                .GroupBy(g => g.EventId)
                .Select(x => new { EventId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.EventId, x => x.Count);
            var photoCounts = await db.Photos
                .Where(p => eventIds.Contains(p.EventId))
                .GroupBy(p => p.EventId)
                .Select(x => new { EventId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.EventId, x => x.Count);

            var result = events.Select(ev => new
            {
                ev.Id,
                ev.Code,
                ev.Name,
                ev.Tier,
                ev.UploadOpensAt,
                ev.UploadClosesAt,
                ev.RevealAt,
                uploadOpen = ev.UploadOpen(now),
                revealed = ev.Revealed(now),
                guestCount = guestCounts.GetValueOrDefault(ev.Id),
                photoCount = photoCounts.GetValueOrDefault(ev.Id),
            });

            return Results.Ok(result);
        })
        .RequireAuthorization("Host")
        .WithName("ListEvents");

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateEventRequest req, ClaimsPrincipal user, PicknicDbContext db) =>
        {
            if (ValidateEventInput(req.Name, req.UploadOpensAt, req.UploadClosesAt, req.RevealAt) is { } invalid)
                return invalid;

            var (ev, error) = await LoadOwnedEvent(id, user, db);
            if (error is not null) return error;

            ev!.Name = req.Name.Trim();
            ev.UploadOpensAt = req.UploadOpensAt;
            ev.UploadClosesAt = req.UploadClosesAt;
            ev.RevealAt = req.RevealAt;
            await db.SaveChangesAsync();

            var now = DateTimeOffset.UtcNow;
            return Results.Ok(new
            {
                ev.Id,
                ev.Code,
                ev.Name,
                ev.Tier,
                ev.UploadOpensAt,
                ev.UploadClosesAt,
                ev.RevealAt,
                uploadOpen = ev.UploadOpen(now),
                revealed = ev.Revealed(now),
            });
        })
        .RequireAuthorization("Host")
        .WithName("UpdateEvent");

        group.MapDelete("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, PicknicDbContext db, BlobSasService blobs) =>
        {
            var (ev, error) = await LoadOwnedEvent(id, user, db);
            if (error is not null) return error;

            var photos = await db.Photos.Where(p => p.EventId == id).ToListAsync();
            if (blobs.Enabled)
                foreach (var p in photos)
                    await blobs.DeleteWithDerivativeAsync(p.BlobPath);

            var guests = await db.Guests.Where(g => g.EventId == id).ToListAsync();
            var invites = await db.Invites.Where(i => i.EventId == id).ToListAsync();

            db.Photos.RemoveRange(photos);
            db.Guests.RemoveRange(guests);
            db.Invites.RemoveRange(invites);
            db.Events.Remove(ev!);
            await db.SaveChangesAsync();

            return Results.Ok(new { deleted = id, photosDeleted = photos.Count });
        })
        .RequireAuthorization("Host")
        .WithName("DeleteEvent");

        return app;
    }

    private static IResult? ValidateEventInput(
        string name, DateTimeOffset opensAt, DateTimeOffset closesAt, DateTimeOffset revealAt)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name))
            errors["name"] = ["A name is required."];
        else if (name.Trim().Length > Event.NameMaxLength)
            errors["name"] = [$"Name must be {Event.NameMaxLength} characters or fewer."];
        if (opensAt >= closesAt)
            errors["uploadOpensAt"] = ["The upload window must open before it closes."];
        else if (closesAt - opensAt > Event.MaxUploadWindow)
            errors["uploadClosesAt"] = [$"A party can run for up to {Event.MaxUploadWindow.TotalDays:0} days."];
        if (revealAt < closesAt)
            errors["revealAt"] = ["The reveal must be at or after the upload window closes."];
        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    private static async Task<(Event? Event, IResult? Error)> LoadOwnedEvent(
        Guid id, ClaimsPrincipal user, PicknicDbContext db)
    {
        var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return (null, Results.NotFound());
        if (ev.HostId != hostId) return (null, Results.Forbid());
        return (ev, null);
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
