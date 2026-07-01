using Microsoft.EntityFrameworkCore;
using Picknic.Api.Data;
using Picknic.Api.Email;

namespace Picknic.Api.Notifications;

/// <summary>
/// Polls for events whose reveal time has passed and emails their guests once.
/// Stamps <c>RevealNotifiedAt</c> before sending so a crash or a second replica
/// can't double-notify.
/// </summary>
public class RevealNotificationService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<RevealNotificationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        try
        {
            do
            {
                await NotifyDueEventsAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutting down — expected.
        }
    }

    private async Task NotifyDueEventsAsync(CancellationToken ct)
    {
        var webBase = config["Web:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PicknicDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now = DateTimeOffset.UtcNow;
        var due = await db.Events
            .Where(e => e.RevealAt <= now && e.RevealNotifiedAt == null)
            .ToListAsync(ct);

        foreach (var ev in due)
        {
            try
            {
                // Stamp + save first so a crash mid-send doesn't re-notify.
                ev.RevealNotifiedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);

                var guests = await db.Guests
                    .Where(g => g.EventId == ev.Id && g.RemovedAt == null && g.Email != null)
                    .ToListAsync(ct);

                var link = $"{webBase}/gallery/{ev.Code}";
                var subject = "Your Picknic photos have developed";
                var body =
                    $"<p>The roll for <strong>{ev.Name}</strong> has developed!</p>" +
                    $"<p><a href=\"{link}\">View the photos</a></p>";

                foreach (var guest in guests)
                {
                    await email.SendAsync(guest.Email!, subject, body);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to notify guests for event {EventId}", ev.Id);
            }
        }
    }
}
