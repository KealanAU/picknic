namespace Picknic.Api.Models;

public class Guest
{
    public const int DisplayNameMaxLength = 80;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public required string DisplayName { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Set when the host removes the guest — revokes their still-valid token.</summary>
    public DateTimeOffset? RemovedAt { get; set; }

    /// <summary>
    /// The host's own upload identity, minted by the camera-pass endpoint so the
    /// host shoots through the same guest-token pipeline. Excluded from the
    /// guest-seat cap.
    /// </summary>
    public bool IsHost { get; set; }
}
