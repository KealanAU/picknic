using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Data;
using Picknic.Api.FilmProcessing;

namespace Picknic.Api.Endpoints;

public static class FilmEndpoints
{
    public static IEndpointRouteBuilder MapFilmEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/film/stocks", () =>
            Results.Ok(FilmStocks.Catalog.Select(s => new { s.Id, s.DisplayName })))
            .WithName("FilmStocks");

        app.MapGet("/api/film/prints", () =>
            Results.Ok(PrintStyles.Catalog.Select(p => new { p.Id, p.DisplayName })))
            .WithName("FilmPrints");

        // Re-running overwrites the derivative; the original blob is never touched.
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

            var stockId = string.IsNullOrWhiteSpace(stock) ? FilmStocks.Default : stock;
            if (!FilmStocks.Exists(stockId))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["stock"] = [$"Unknown film stock '{stock}'."],
                });
            var printId = string.IsNullOrWhiteSpace(print) ? PrintStyles.None : print;
            if (printId != PrintStyles.None && PrintStyles.Resolve(printId) is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["print"] = [$"Unknown print style '{print}'."],
                });

            var devPath = await developer.DevelopAsync(photo.BlobPath, stockId, printId);
            if (devPath is null)
                return Results.Problem("Could not develop photo (storage disabled or blob missing).", statusCode: 409);

            photo.DevelopedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { photoId, stock = stockId, print = printId, developed = true });
        })
        .RequireAuthorization("Host")
        .WithName("DevelopPhoto");

        return app;
    }
}
