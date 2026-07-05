using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Picknic.Api.Auth;
using Picknic.Api.Data;
using Picknic.Api.Email;
using Picknic.Api.Models;

namespace Picknic.Api.Endpoints;

public record InviteRequest(string[] Emails);

public static class InviteEndpoints
{
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        // Host emails guests a join link. Re-inviting an existing address is a no-op.
        app.MapPost("/api/events/{id:guid}/invites", async (
            Guid id, InviteRequest req, ClaimsPrincipal user, PicknicDbContext db,
            JoinSecretProtector secrets, EventLinks links, IEmailSender email) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();
            if (ev.HostId != hostId) return Results.Forbid();

            var joinSecret = secrets.UnprotectOrRotate(ev, out _);
            var joinUrl = links.JoinUrl(ev.Code, joinSecret);
            var existing = await db.Invites.Where(i => i.EventId == id)
                .Select(i => i.Email).ToListAsync();

            var added = 0;
            foreach (var address in req.Emails.Select(e => e.Trim()).Where(e => e.Length > 0).Distinct())
            {
                if (existing.Contains(address)) continue;
                db.Invites.Add(new Invite { EventId = id, Email = address });
                var body = $"<p>You're invited to <strong>{ev.Name}</strong>.</p>"
                    + $"<p><a href='{joinUrl}'>Tap here to join and start snapping.</a></p>";
                await email.SendAsync(address, $"You're invited to {ev.Name}", body);
                added++;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { invited = added });
        })
        .RequireAuthorization("Host")
        .WithName("InviteGuests");

        return app;
    }
}
