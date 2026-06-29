using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Auth;
using Picknic.Api.Data;
using Picknic.Api.Models;
using Picknic.Api.Storage;

namespace Picknic.Api.Endpoints;

public record CompleteUploadRequest(string BlobPath, string? Caption);

public static class UploadEndpoints
{
    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events/{id:guid}");

        // Guest asks for a one-shot upload URL. Re-checks the window server-side
        // and binds the SAS expiry to the deadline.
        group.MapPost("/uploads", async (
            Guid id, ClaimsPrincipal user, PicknicDbContext db, BlobSasService blobs) =>
        {
            if (!GuestOwnsEvent(user, id)) return Results.Forbid();
            if (!blobs.Enabled) return Results.Problem("Storage is not configured.", statusCode: 501);

            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();

            var now = DateTimeOffset.UtcNow;
            if (!ev.UploadOpen(now))
                return Results.Problem("Uploads are closed for this event.", statusCode: 403);

            // SAS dies at the window close (or sooner) — Azure enforces it.
            var expiry = now.AddMinutes(5) < ev.UploadClosesAt ? now.AddMinutes(5) : ev.UploadClosesAt;
            var target = await blobs.CreateUploadSasAsync(id, expiry);
            return Results.Ok(target);
        })
        .RequireAuthorization("Guest")
        .WithName("CreateUpload");

        // Guest confirms the blob landed; server validates size + ownership and
        // records metadata. (Alternatively, react to a Blob Created event.)
        group.MapPost("/uploads/complete", async (
            Guid id, CompleteUploadRequest req, ClaimsPrincipal user,
            PicknicDbContext db, BlobSasService blobs) =>
        {
            if (!GuestOwnsEvent(user, id)) return Results.Forbid();
            if (!req.BlobPath.StartsWith($"{id}/")) return Results.BadRequest("Blob path outside event.");

            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();

            var size = await blobs.GetSizeAsync(req.BlobPath);
            if (size is null) return Results.BadRequest("Blob not found.");

            var photo = new Photo
            {
                EventId = id,
                BlobPath = req.BlobPath,
                Caption = req.Caption,
                SizeBytes = size.Value,
            };
            db.Photos.Add(photo);
            await db.SaveChangesAsync();
            return Results.Ok(new { photo.Id });
        })
        .RequireAuthorization("Guest")
        .WithName("CompleteUpload");

        // Reveal gallery — only after RevealAt, served as short read-SAS links.
        group.MapGet("/photos", async (Guid id, PicknicDbContext db, BlobSasService blobs) =>
        {
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();

            var now = DateTimeOffset.UtcNow;
            if (!ev.Revealed(now))
                return Results.Ok(new { revealed = false, ev.RevealAt });

            var photos = await db.Photos.Where(p => p.EventId == id).ToListAsync();
            var expiry = now.AddHours(1);
            var items = blobs.Enabled
                ? await Task.WhenAll(photos.Select(async p => new
                {
                    p.Id,
                    p.Caption,
                    url = (string?)await blobs.CreateReadSasAsync(p.BlobPath, expiry),
                }))
                : photos.Select(p => new { p.Id, p.Caption, url = (string?)null }).ToArray();

            return Results.Ok(new { revealed = true, photos = items });
        })
        .WithName("GetPhotos");

        return app;
    }

    private static bool GuestOwnsEvent(ClaimsPrincipal user, Guid eventId) =>
        user.FindFirstValue(GuestTokenService.EventClaim) == eventId.ToString();
}
