namespace Picknic.Api.Models;

public record Event(
    string Id,
    string Code,
    string Name,
    DateTimeOffset RevealAt)
{
    public List<Photo> Photos { get; init; } = [];
}

public record Photo(
    string Id,
    string Url,
    string? Caption,
    DateTimeOffset TakenAt);

public record CreateEvent(
    string Code,
    string Name,
    DateTimeOffset RevealAt);
