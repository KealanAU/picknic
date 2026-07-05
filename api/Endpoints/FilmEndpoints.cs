using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Data;
using Picknic.Api.FilmProcessing;

namespace Picknic.Api.Endpoints;

/// <summary>
/// Film-look endpoints: the selectable stock catalogue, and a host-triggered
/// develop for a single photo. Kept separate from <see cref="UploadEndpoints"/>
/// so the capture/upload path and the develop path stay independently evolvable.
/// </summary>
public static class FilmEndpoints
{
    public static IEndpointRouteBuilder MapFilmEndpoints(this IEndpointRouteBuilder app)
    {
        // Public catalogues for a client-side "choose your roll" picker.
        app.MapGet("/api/film/stocks", () =>
            Results.Ok(FilmStocks.Catalog.Select(s => new { s.Id, s.DisplayName })))
            .WithName("FilmStocks");

        app.MapGet("/api/film/prints", () =>
            Results.Ok(PrintStyles.Catalog.Select(p => new { p.Id, p.DisplayName })))
            .WithName("FilmPrints");

        // Host develops one photo with a chosen stock + optional instant-print
        // frame. Idempotent-ish: re-running overwrites the derivative. The original
        // blob is never touched.
        app.MapPost("/api/events/{id:guid}/photos/{photoId:guid}/develop", async (
            Guid id, Guid photoId, string? stock, string? print, ClaimsPrincipal user,
            PicknicDbContext db, FilmDeveloper developer) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();
            if (ev.HostId != hostId) return Results.Forbid();

            var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == photoId && p.EventId == id);
            if (photo is null) return Results.NotFound();

            var stockId = stock ?? FilmStocks.Default;
            var devPath = await developer.DevelopAsync(photo.BlobPath, stockId, print);
            return devPath is null
                ? Results.Problem("Could not develop photo (storage disabled or blob missing).", statusCode: 409)
                : Results.Ok(new { photoId, stock = stockId, print = print ?? PrintStyles.None, developed = true });
        })
        .RequireAuthorization("Host")
        .WithName("DevelopPhoto");

        return app;
    }
}
