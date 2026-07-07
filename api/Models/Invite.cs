namespace Picknic.Api.Models;

public class Invite
{
    public const int EmailMaxLength = 256;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public required string Email { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; set; }
}
