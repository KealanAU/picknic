using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Picknic.Api.Data;
using Picknic.Api.Storage;

namespace Picknic.Api.Endpoints;

public class EventGridOptions
{
    public const string SectionName = "EventGrid";

    /// <summary>Shared secret Event Grid must present as ?code=… — empty disables the check (dev).</summary>
    public string Secret { get; set; } = string.Empty;
}

/// <summary>
/// Receives Azure Storage <c>BlobCreated</c> events so a photo is registered only
/// once its blob has actually landed — the robust path that doesn't rely on the
/// guest's client calling back. Guest/event are read from the blob path
/// (<c>{eventId}/{guestId}/{file}</c>) minted at SAS time.
/// </summary>
public static class EventGridEndpoints
{
    public static IEndpointRouteBuilder MapEventGridEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/uploads/events", async (
            HttpRequest request, [FromQuery] string? code,
            IOptions<EventGridOptions> egOpts, PicknicDbContext db, BlobSasService blobs,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("EventGrid");
            var secret = egOpts.Value.Secret;
            if (!string.IsNullOrEmpty(secret) && code != secret)
                return Results.Unauthorized();

            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();

            EventGridEvent[] events;
            try
            {
                events = EventGridEvent.ParseMany(BinaryData.FromString(json));
            }
            catch
            {
                return Results.BadRequest();
            }

            foreach (var gridEvent in events)
            {
                if (!gridEvent.TryGetSystemEventData(out var data)) continue;

                // Event Grid's one-time endpoint validation handshake.
                if (data is SubscriptionValidationEventData validation)
                    return Results.Ok(new SubscriptionValidationResponse
                    {
                        ValidationResponse = validation.ValidationCode,
                    });

                if (data is StorageBlobCreatedEventData)
                {
                    // Never 500 the batch on a single bad/poison event — Event Grid
                    // would retry it forever. Log and move on; the idempotent client
                    // callback covers anything genuinely missed.
                    try
                    {
                        var blobPath = BlobPathFromSubject(gridEvent.Subject, blobs.Container);
                        if (blobPath is null || !TryParseIds(blobPath, out var eventId, out var guestId))
                            continue;

                        // Only register for a guest that still exists and hasn't been
                        // kicked; otherwise drop the orphaned blob.
                        var guest = await db.Guests.FirstOrDefaultAsync(
                            g => g.Id == guestId && g.EventId == eventId && g.RemovedAt == null);
                        if (guest is null)
                        {
                            if (blobs.Enabled) await blobs.DeleteAsync(blobPath);
                            continue;
                        }

                        await UploadEndpoints.RegisterPhotoAsync(db, blobs, eventId, guestId, blobPath, caption: null);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to process BlobCreated for {Subject}", gridEvent.Subject);
                    }
                }
            }

            return Results.Ok();
        })
        .WithName("StorageBlobEvents");

        return app;
    }

    // Subject: /blobServices/default/containers/{container}/blobs/{eventId}/{guestId}/{file}
    private static string? BlobPathFromSubject(string subject, string container)
    {
        var marker = $"/containers/{container}/blobs/";
        var idx = subject.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return idx < 0 ? null : subject[(idx + marker.Length)..];
    }

    private static bool TryParseIds(string blobPath, out Guid eventId, out Guid guestId)
    {
        eventId = guestId = Guid.Empty;
        var parts = blobPath.Split('/');
        return parts.Length >= 3
            && Guid.TryParse(parts[0], out eventId)
            && Guid.TryParse(parts[1], out guestId);
    }
}
