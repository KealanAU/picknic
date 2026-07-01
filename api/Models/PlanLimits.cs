namespace Picknic.Api.Models;

/// <summary>
/// Per-tier caps enforced at join (guests) and upload (photos per guest).
/// Tier names match the Stripe checkout plans; "free" is the default for a new
/// event. An unknown/legacy tier falls back to the free limits.
/// </summary>
public static class PlanLimits
{
    public record Limits(int MaxGuests, int MaxPhotosPerGuest);

    private static readonly Dictionary<string, Limits> ByTier = new(StringComparer.OrdinalIgnoreCase)
    {
        ["free"] = new(MaxGuests: 25, MaxPhotosPerGuest: 30),
        ["premium"] = new(MaxGuests: 150, MaxPhotosPerGuest: 200),
        ["pro"] = new(MaxGuests: int.MaxValue, MaxPhotosPerGuest: int.MaxValue),
    };

    public static Limits For(string? tier) =>
        tier is not null && ByTier.TryGetValue(tier, out var l) ? l : ByTier["free"];
}
