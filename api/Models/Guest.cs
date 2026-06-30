namespace Picknic.Api.Models;

public class Guest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public required string DisplayName { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
