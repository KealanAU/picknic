using System.Net;
using System.Net.Mail;
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
    private const int MaxEmailsPerRequest = 50;

    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        // Re-inviting an existing address is a no-op.
        app.MapPost("/api/events/{id:guid}/invites", async (
            Guid id, InviteRequest req, ClaimsPrincipal user, PicknicDbContext db,
            JoinSecretProtector secrets, EventLinks links, IEmailSender email) =>
        {
            var hostId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var ev = await db.Events.FindAsync(id);
            if (ev is null) return Results.NotFound();
            if (ev.HostId != hostId) return Results.Forbid();

            var addresses = req.Emails.Select(e => e.Trim()).Where(e => e.Length > 0).Distinct().ToList();
            if (addresses.Count > MaxEmailsPerRequest)
                return Results.Problem(
                    $"You can invite at most {MaxEmailsPerRequest} guests at a time.", statusCode: 400);

            var joinSecret = secrets.UnprotectOrRotate(ev, out _);
            var joinUrl = links.JoinUrl(ev.Code, joinSecret);
            // The name is host-controlled — encode it before it lands in the email HTML.
            var eventName = WebUtility.HtmlEncode(ev.Name);
            var existing = await db.Invites.Where(i => i.EventId == id)
                .Select(i => i.Email).ToListAsync();

            var added = 0;
            var skipped = 0;
            foreach (var address in addresses)
            {
                if (address.Length > Invite.EmailMaxLength || !IsValidEmail(address)) { skipped++; continue; }
                if (existing.Contains(address)) continue;
                db.Invites.Add(new Invite { EventId = id, Email = address });
                var body = $"<p>You're invited to <strong>{eventName}</strong>.</p>"
                    + $"<p><a href='{joinUrl}'>Tap here to join and start snapping.</a></p>";
                await email.SendAsync(address, $"You're invited to {ev.Name}", body);
                added++;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { invited = added, skipped });
        })
        .RequireAuthorization("Host")
        .RequireRateLimiting("invite")
        .WithName("InviteGuests");

        return app;
    }

    // TryCreate is lenient (accepts "Name <addr>" forms), so require the parse to
    // round-trip to the exact input — a bare address and nothing else.
    private static bool IsValidEmail(string address) =>
        MailAddress.TryCreate(address, out var parsed) && parsed.Address == address;
}
