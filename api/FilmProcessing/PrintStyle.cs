namespace Picknic.Api.FilmProcessing;

/// <summary>
/// An instant-print layout: the photo window aspect plus the paper borders around
/// it (the thick bottom margin is the Polaroid/Instax signature). Borders are
/// expressed as a fraction of the photo-window width so a print looks identical at
/// any resolution.
/// </summary>
public sealed record PrintStyle(
    string Id,
    string DisplayName,
    float PhotoAspect,   // window width / height
    float SideBorder,    // fraction of window width
    float TopBorder,     // fraction of window width
    float BottomBorder,  // fraction of window width (larger — the caption strip)
    (float R, float G, float B) Paper);

/// <summary>The instant-print looks a roll can be framed with. <c>none</c> = bare photo.</summary>
public static class PrintStyles
{
    public const string None = "none";

    private static readonly PrintStyle[] All =
    {
        new("polaroid", "Polaroid", 1f, 0.055f, 0.055f, 0.24f, (0.97f, 0.965f, 0.94f)),
        new("instax_mini", "Instax Mini", 46f / 62f, 0.07f, 0.07f, 0.21f, (0.995f, 0.995f, 0.99f)),
        new("instax_square", "Instax Square", 1f, 0.075f, 0.075f, 0.22f, (0.995f, 0.995f, 0.99f)),
        new("instax_wide", "Instax Wide", 99f / 62f, 0.05f, 0.05f, 0.16f, (0.995f, 0.995f, 0.99f)),
    };

    /// <summary>Ids/names for a client picker (plus the implicit "none").</summary>
    public static IReadOnlyList<(string Id, string DisplayName)> Catalog { get; } =
        new (string, string)[] { (None, "No frame") }
            .Concat(All.Select(p => (p.Id, p.DisplayName))).ToList();

    /// <summary>Resolves a style id; null for "none"/unknown (leave the photo bare).</summary>
    public static PrintStyle? Resolve(string? id) =>
        string.IsNullOrWhiteSpace(id) || id == None
            ? null
            : All.FirstOrDefault(p => p.Id == id);
}
