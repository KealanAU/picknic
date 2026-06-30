using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Auth;
using Picknic.Api.Data;
using Picknic.Api.Models;

namespace Picknic.Api.Endpoints;

public record CreateEventRequest(
    string Name,
    DateTimeOffset UploadOpensAt,
    DateTimeOffset UploadClosesAt,
    DateTimeOffset RevealAt);

public record JoinRequest(string JoinSecret);

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events");

        // The join secret is returned once here and only ever stored hashed.
        group.MapPost("/", async (CreateEventRequest req, ClaimsPrincipal user, PicknicDbContext db) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (hostId is null) return Results.Unauthorized();

            var secret = GuestTokenService.NewSecret();
            var ev = new Event
            {
                Code = NewCode(),
                Name = req.Name,
                JoinSecretHash = GuestTokenService.Hash(secret),
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

        group.MapPost("/{code}/join", async (
            string code, JoinRequest req, PicknicDbContext db, GuestTokenService tokens) =>
        {
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Code == code.ToUpperInvariant());
            if (ev is null) return Results.NotFound();

            if (!GuestTokenService.VerifyHash(req.JoinSecret, ev.JoinSecretHash))
                return Results.Unauthorized();

            var now = DateTimeOffset.UtcNow;
            if (!ev.UploadOpen(now))
                return Results.Problem("Uploads are closed for this event.", statusCode: 403);

            var guestId = Guid.NewGuid();
            return Results.Ok(new
            {
                guestId,
                token = tokens.Issue(ev.Id, guestId, ev.UploadClosesAt),
                expiresAt = ev.UploadClosesAt,
                eventId = ev.Id,
            });
        })
        .WithName("JoinEvent");

        return app;
    }

    private static string NewCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Concat(Enumerable.Range(0, 6)
            .Select(_ => alphabet[Random.Shared.Next(alphabet.Length)]));
    }
}
