using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Auth;
using Picknic.Api.Data;
using Picknic.Api.FilmProcessing;
using Picknic.Api.Models;
using Picknic.Api.Storage;

namespace Picknic.Api.Endpoints;

public record CompleteUploadRequest(string BlobPath, string? Caption);

public static class UploadEndpoints
{
    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events/{id:guid}");

        group.MapPost("/uploads", async (
            Guid id, ClaimsPrincipal user, PicknicDbContext db, BlobSasService blobs) =>
        {
            var guest = await ActiveGuest(db, user, id);
            if (guest is null) return Results.Forbid();
            if (!blobs.Enabled) return Results.Problem("Storage is not configured.", statusCode: 501);

            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();

            var now = DateTimeOffset.UtcNow;
            if (!ev.UploadOpen(now))
                return Results.Problem("Uploads are closed for this event.", statusCode: 403);

            // SAS dies at the window close (or sooner) — Azure enforces it.
            var expiry = now.AddMinutes(5) < ev.UploadClosesAt ? now.AddMinutes(5) : ev.UploadClosesAt;
            var target = await blobs.CreateUploadSasAsync(id, guest.Id, expiry);
            return Results.Ok(target);
        })
        .RequireAuthorization("Guest")
        .RequireRateLimiting("upload")
        .WithName("CreateUpload");

        // The authoritative registration path is the Event Grid BlobCreated handler
        // (see EventGridEndpoints); this callback is a client-driven fallback for
        // dev/local where Event Grid isn't wired, and it also carries the caption.
        // Both funnel through RegisterPhotoAsync, which is idempotent per blob.
        group.MapPost("/uploads/complete", async (
            Guid id, CompleteUploadRequest req, ClaimsPrincipal user,
            PicknicDbContext db, BlobSasService blobs) =>
        {
            var guest = await ActiveGuest(db, user, id);
            if (guest is null) return Results.Forbid();
            if (!IsMintedBlobPath(req.BlobPath, id, guest.Id))
                return Results.Problem("Blob path does not belong to this guest.", statusCode: 400);
            if (req.Caption is { Length: > Photo.CaptionMaxLength })
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["caption"] = [$"Caption must be {Photo.CaptionMaxLength} characters or fewer."],
                });

            return await RegisterPhotoAsync(db, blobs, id, guest.Id, req.BlobPath, req.Caption);
        })
        .RequireAuthorization("Guest")
        .RequireRateLimiting("upload")
        .WithName("CompleteUpload");

        group.MapDelete("/photos/{photoId:guid}", async (
            Guid id, Guid photoId, ClaimsPrincipal user,
            PicknicDbContext db, BlobSasService blobs) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();
            if (ev.HostId != hostId) return Results.Forbid();

            var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == photoId && p.EventId == id);
            if (photo is null) return Results.NotFound();

            if (blobs.Enabled)
                await blobs.DeleteWithDerivativeAsync(photo.BlobPath);
            db.Photos.Remove(photo);
            await db.SaveChangesAsync();
            return Results.Ok(new { deleted = photo.Id });
        })
        .RequireAuthorization("Host")
        .WithName("DeletePhoto");

        group.MapGet("/photos", async (Guid id, PicknicDbContext db, BlobSasService blobs) =>
        {
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();

            var now = DateTimeOffset.UtcNow;
            if (!ev.Revealed(now))
                return Results.Ok(new { revealed = false, ev.RevealAt });

            var photos = await db.Photos.Where(p => p.EventId == id).ToListAsync();
            var names = await db.Guests.Where(g => g.EventId == id)
                .ToDictionaryAsync(g => g.Id, g => g.DisplayName);
            var expiry = now.AddHours(1);

            // Serve the developed (film-look) derivative when it exists, else the
            // original. Keeps develop fully optional and decoupled from upload.
            // DevelopedAt (stamped by the develop endpoint) replaces a per-photo
            // blob existence probe.
            async Task<string?> ReadUrl(Photo p) => await blobs.CreateReadSasAsync(
                p.DevelopedAt is not null ? FilmDeveloper.DevelopedPath(p.BlobPath) : p.BlobPath,
                expiry);

            var items = blobs.Enabled
                ? await Task.WhenAll(photos.Select(async p => new
                {
                    p.Id,
                    p.Caption,
                    p.UploadedByGuestId,
                    uploadedBy = names.GetValueOrDefault(p.UploadedByGuestId),
                    url = await ReadUrl(p),
                }))
                : photos.Select(p => new
                {
                    p.Id,
                    p.Caption,
                    p.UploadedByGuestId,
                    uploadedBy = names.GetValueOrDefault(p.UploadedByGuestId),
                    url = (string?)null,
                }).ToArray();

            return Results.Ok(new { revealed = true, photos = items });
        })
        .RequireRateLimiting("photos")
        .WithName("GetPhotos");

        return app;
    }

    // Registers a landed blob as a Photo, validating type/size/plan cap. Idempotent
    // on BlobPath so the Event Grid handler and the client callback can both fire
    // for the same upload without creating duplicates. Deletes the blob on rejection.
    internal static async Task<IResult> RegisterPhotoAsync(
        PicknicDbContext db, BlobSasService blobs,
        Guid eventId, Guid guestId, string blobPath, string? caption)
    {
        var existing = await db.Photos.FirstOrDefaultAsync(p => p.BlobPath == blobPath);
        if (existing is not null)
        {
            // Late-arriving caption from the client callback fills in a blank.
            if (caption is not null && existing.Caption is null)
            {
                existing.Caption = caption;
                await db.SaveChangesAsync();
            }
            return Results.Ok(new { existing.Id });
        }

        var ev = await db.Events.FindAsync(eventId);
        if (ev is null) return Results.NotFound();

        var info = await blobs.GetBlobInfoAsync(blobPath);
        if (info is null) return Results.Problem("Blob not found.", statusCode: 400);
        var (size, contentType) = info.Value;

        if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            await blobs.DeleteAsync(blobPath);
            return Results.Problem("Only image uploads are allowed.", statusCode: 400);
        }

        if (size > blobs.MaxBytes)
        {
            await blobs.DeleteAsync(blobPath);
            return Results.Problem("Photo exceeds the maximum allowed size.", statusCode: 400);
        }

        var cap = PlanLimits.For(ev.Tier).MaxPhotosPerGuest;
        var count = await db.Photos.CountAsync(
            p => p.EventId == eventId && p.UploadedByGuestId == guestId);
        if (count >= cap)
        {
            await blobs.DeleteAsync(blobPath);
            return Results.Problem("Photo limit reached for this event.", statusCode: 403);
        }

        var photo = new Photo
        {
            EventId = eventId,
            BlobPath = blobPath,
            UploadedByGuestId = guestId,
            Caption = caption,
            SizeBytes = size,
        };
        db.Photos.Add(photo);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A concurrent registration of the same blob won the unique index race.
            return Results.Ok();
        }
        return Results.Ok(new { photo.Id });
    }

    // Upload SAS mints exactly "{eventId}/{guestId}/{Guid:n}.jpg"; the complete
    // callback must name one of those, not an arbitrary path. Pinning the whole
    // shape (not just the prefix) rejects "../" traversal, another guest's blobs,
    // and the ".dev.jpg" derivatives.
    internal static bool IsMintedBlobPath(string blobPath, Guid eventId, Guid guestId)
    {
        var prefix = $"{eventId}/{guestId}/";
        if (!blobPath.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var name = blobPath[prefix.Length..];
        return name.EndsWith(".jpg", StringComparison.Ordinal)
            && Guid.TryParseExact(name[..^4], "N", out _);
    }

    // The token must be scoped to this event AND the guest must still be active —
    // this is where a host's kick takes effect on an otherwise-valid token.
    private static async Task<Guest?> ActiveGuest(PicknicDbContext db, ClaimsPrincipal user, Guid eventId)
    {
        if (user.FindFirstValue(GuestTokenService.EventClaim) != eventId.ToString()) return null;
        var guestId = GuestTokenService.GuestId(user);
        if (guestId is null) return null;
        return await db.Guests.FirstOrDefaultAsync(
            g => g.Id == guestId && g.EventId == eventId && g.RemovedAt == null);
    }
}
