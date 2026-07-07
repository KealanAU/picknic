namespace Picknic.Api.Models;

/// <summary>
/// An event is a "roll": guests upload during an open window, photos are
/// revealed once the roll develops. The upload deadline is enforced in three
/// layers — guest token expiry, the API gate here, and the blob SAS expiry.
/// </summary>
public class Event
{
    public const int NameMaxLength = 120;

    /// <summary>
    /// Parties can span multiple days (weddings, festivals), but an unbounded
    /// window would keep join links and guest tokens live indefinitely.
    /// </summary>
    public static readonly TimeSpan MaxUploadWindow = TimeSpan.FromDays(31);

    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-friendly code shown on invites (low entropy — not a secret).</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>The QR join secret, encrypted at rest so the host can re-display the QR.</summary>
    public required string JoinSecretEnc { get; set; }

    public DateTimeOffset UploadOpensAt { get; set; }
    public DateTimeOffset UploadClosesAt { get; set; }
    public DateTimeOffset RevealAt { get; set; }

    public required string HostId { get; set; }

    /// <summary>Plan tier — "free" until a Stripe checkout is fulfilled. Drives <see cref="PlanLimits"/>.</summary>
    public string Tier { get; set; } = "free";

    /// <summary>Set by the Stripe webhook when checkout for an upgrade completes.</summary>
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>Set once guests have been notified the roll developed — prevents double-sends.</summary>
    public DateTimeOffset? RevealNotifiedAt { get; set; }

    public List<Photo> Photos { get; set; } = [];

    public bool UploadOpen(DateTimeOffset now) =>
        now >= UploadOpensAt && now < UploadClosesAt;

    public bool Revealed(DateTimeOffset now) => now >= RevealAt;
}

public class Photo
{
    /// <summary>App-level cap; the column is unbounded text.</summary>
    public const int CaptionMaxLength = 500;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }

    /// <summary>Server-generated path: "{eventId}/{guid}.jpg" — never the client filename.</summary>
    public required string BlobPath { get; set; }

    public Guid UploadedByGuestId { get; set; }

    public string? Caption { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Set when a developed derivative exists — saves a per-photo blob existence probe.</summary>
    public DateTimeOffset? DevelopedAt { get; set; }
}
