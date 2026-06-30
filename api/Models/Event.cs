namespace Picknic.Api.Models;

/// <summary>
/// An event is a "roll": guests upload during an open window, photos are
/// revealed once the roll develops. The upload deadline is enforced in three
/// layers — guest token expiry, the API gate here, and the blob SAS expiry.
/// </summary>
public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-friendly code shown on invites (low entropy — not a secret).</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>SHA-256 of the high-entropy join secret carried by the QR.</summary>
    public required string JoinSecretHash { get; set; }

    public DateTimeOffset UploadOpensAt { get; set; }
    public DateTimeOffset UploadClosesAt { get; set; }
    public DateTimeOffset RevealAt { get; set; }

    public required string HostId { get; set; }

    public List<Photo> Photos { get; set; } = [];

    public bool UploadOpen(DateTimeOffset now) =>
        now >= UploadOpensAt && now < UploadClosesAt;

    public bool Revealed(DateTimeOffset now) => now >= RevealAt;
}

public class Photo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }

    /// <summary>Server-generated path: "{eventId}/{guid}.jpg" — never the client filename.</summary>
    public required string BlobPath { get; set; }

    public Guid UploadedByGuestId { get; set; }

    public string? Caption { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
