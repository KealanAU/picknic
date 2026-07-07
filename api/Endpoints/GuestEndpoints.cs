using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Data;
using Picknic.Api.Storage;

namespace Picknic.Api.Endpoints;

public static class GuestEndpoints
{
    public static IEndpointRouteBuilder MapGuestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events/{id:guid}/guests");

        group.MapGet("/", async (Guid id, ClaimsPrincipal user, PicknicDbContext db) =>
        {
            if (!await HostOwns(db, id, user)) return Results.Forbid();

            var guests = await db.Guests.Where(g => g.EventId == id)
                .OrderBy(g => g.JoinedAt)
                .Select(g => new
                {
                    g.Id,
                    g.DisplayName,
                    g.Email,
                    g.JoinedAt,
                    removed = g.RemovedAt != null,
                    photos = db.Photos.Count(p => p.UploadedByGuestId == g.Id),
                })
                .ToListAsync();

            return Results.Ok(guests);
        })
        .RequireAuthorization("Host")
        .WithName("ListGuests");

        group.MapDelete("/{guestId:guid}", async (
            Guid id, Guid guestId, ClaimsPrincipal user,
            PicknicDbContext db, BlobSasService blobs) =>
        {
            if (!await HostOwns(db, id, user)) return Results.Forbid();

            var guest = await db.Guests.FirstOrDefaultAsync(g => g.Id == guestId && g.EventId == id);
            if (guest is null) return Results.NotFound();

            guest.RemovedAt = DateTimeOffset.UtcNow;

            var photos = await db.Photos.Where(p => p.UploadedByGuestId == guestId).ToListAsync();
            if (blobs.Enabled)
                foreach (var p in photos)
                    await blobs.DeleteWithDerivativeAsync(p.BlobPath);
            db.Photos.RemoveRange(photos);

            await db.SaveChangesAsync();
            return Results.Ok(new { removed = guest.Id, photosDeleted = photos.Count });
        })
        .RequireAuthorization("Host")
        .WithName("RemoveGuest");

        return app;
    }

    private static async Task<bool> HostOwns(PicknicDbContext db, Guid eventId, ClaimsPrincipal user)
    {
        var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var ev = await db.Events.FindAsync(eventId);
        return ev is not null && ev.HostId == hostId;
    }
}
