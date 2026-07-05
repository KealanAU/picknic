using System.Collections.Concurrent;
using Picknic.Api.FilmProcessing.Filters;

namespace Picknic.Api.FilmProcessing;

public sealed record FilmStock(string Id, string DisplayName, IReadOnlyList<IImageFilter> Chain);

public static class FilmStocks
{
    public const string Default = "portra400";

    private sealed record Def(
        string Name,
        (float R, float G, float B) Lift,
        (float R, float G, float B) Gamma,
        (float R, float G, float B) Gain,
        float Sat,
        float BlackLift,
        float Contrast,
        float? HalThreshold,
        float HalIntensity,
        int HalRadius,
        float Grain,
        float Vignette);

    private static readonly (float, float, float) Neutral = (0f, 0f, 0f);
    private static readonly (float, float, float) One = (1f, 1f, 1f);

    private static readonly IReadOnlyList<(string Id, Def Def)> Defs = new (string, Def)[]
    {
        // Kodak colour negative
        ("portra160", new Def("Portra 160", (0.012f, 0.008f, 0f), One, (1f, 1f, 0.985f), 1.00f, 0.04f, 0.08f, 0.82f, 0.25f, 9, 0.025f, 0.18f)),
        ("portra400", new Def("Portra 400", (0.015f, 0.01f, 0f), (1.02f, 1f, 0.99f), (1f, 1f, 0.98f), 1.03f, 0.045f, 0.10f, 0.78f, 0.30f, 10, 0.035f, 0.22f)),
        ("portra800", new Def("Portra 800", (0.018f, 0.012f, 0f), (1.03f, 1f, 0.98f), (1.02f, 1f, 0.97f), 1.05f, 0.05f, 0.11f, 0.74f, 0.35f, 12, 0.06f, 0.24f)),
        ("gold200", new Def("Kodak Gold 200", (0.01f, 0.005f, -0.01f), (1.05f, 1f, 0.95f), (1.03f, 1f, 0.95f), 1.12f, 0.035f, 0.16f, 0.80f, 0.35f, 10, 0.045f, 0.24f)),
        ("ultramax400", new Def("Kodak UltraMax 400", (0.008f, 0.006f, 0f), (1.03f, 1f, 0.97f), (1.03f, 1.0f, 0.98f), 1.15f, 0.04f, 0.18f, 0.80f, 0.30f, 10, 0.06f, 0.26f)),
        ("colorplus200", new Def("Kodak ColorPlus 200", (0.014f, 0.008f, -0.008f), (1.04f, 1f, 0.97f), (1.02f, 1f, 0.96f), 1.06f, 0.045f, 0.13f, 0.80f, 0.30f, 10, 0.05f, 0.24f)),
        ("ektar100", new Def("Kodak Ektar 100", (0f, 0f, 0f), (1.02f, 1f, 0.98f), (1.04f, 1f, 0.98f), 1.25f, 0.03f, 0.20f, 0.84f, 0.22f, 8, 0.02f, 0.20f)),

        // Kodak slide / cine
        ("kodachrome64", new Def("Kodachrome 64", (0.006f, 0f, -0.006f), (1.04f, 1f, 0.96f), (1.05f, 1f, 0.95f), 1.18f, 0.03f, 0.22f, 0.82f, 0.28f, 9, 0.03f, 0.26f)),
        ("ektachrome100", new Def("Ektachrome E100", (-0.006f, 0f, 0.008f), (1f, 1f, 1.02f), (0.99f, 1f, 1.02f), 1.12f, 0.03f, 0.18f, 0.84f, 0.22f, 8, 0.025f, 0.20f)),

        // Fujifilm
        ("fuji_c200", new Def("Fujicolor C200", (-0.008f, 0.006f, 0.006f), (1f, 1.02f, 1.01f), (0.98f, 1.01f, 1f), 1.08f, 0.045f, 0.14f, 0.82f, 0.25f, 9, 0.05f, 0.22f)),
        ("superia400", new Def("Fujifilm Superia 400", (-0.01f, 0.008f, 0.006f), (1f, 1.03f, 1f), (0.98f, 1.02f, 0.99f), 1.14f, 0.05f, 0.17f, 0.80f, 0.28f, 10, 0.06f, 0.24f)),
        ("pro400h", new Def("Fujifilm Pro 400H", (0.012f, 0.014f, 0.012f), (1.01f, 1.02f, 1.02f), (0.99f, 1f, 1f), 0.98f, 0.055f, 0.07f, 0.80f, 0.30f, 11, 0.035f, 0.16f)),
        ("velvia50", new Def("Fujifilm Velvia 50", (0f, 0f, -0.004f), (1.04f, 1.02f, 0.96f), (1.05f, 1.01f, 0.97f), 1.35f, 0.025f, 0.24f, 0.86f, 0.20f, 8, 0.02f, 0.24f)),
        ("provia100", new Def("Fujifilm Provia 100F", (-0.004f, 0f, 0.004f), (1.01f, 1f, 1.01f), (1f, 1f, 1.01f), 1.10f, 0.03f, 0.16f, 0.84f, 0.22f, 8, 0.025f, 0.20f)),

        // CineStill
        ("cinestill800t", new Def("CineStill 800T", (-0.012f, 0.004f, 0.02f), (1f, 1f, 1.05f), (1f, 0.99f, 0.98f), 0.95f, 0.05f, 0.10f, 0.62f, 0.70f, 16, 0.06f, 0.30f)),
        ("cinestill50d", new Def("CineStill 50D", (0f, 0f, 0.004f), (1.01f, 1f, 1.01f), (1.01f, 1f, 0.99f), 1.05f, 0.04f, 0.12f, 0.70f, 0.40f, 12, 0.03f, 0.22f)),
        ("cinestill400d", new Def("CineStill 400D", (0.004f, 0.004f, 0.004f), (1.02f, 1f, 0.99f), (1.01f, 1f, 0.99f), 1.08f, 0.045f, 0.13f, 0.68f, 0.45f, 12, 0.045f, 0.24f)),

        // Lomography
        ("lomo800", new Def("Lomography 800", (0.006f, 0.004f, -0.006f), (1.03f, 1f, 0.96f), (1.03f, 1f, 0.96f), 1.20f, 0.05f, 0.19f, 0.74f, 0.45f, 14, 0.08f, 0.34f)),

        // Black & white (saturation 0, no halation)
        ("trix400", new Def("Kodak Tri-X 400", Neutral, One, One, 0f, 0.03f, 0.20f, null, 0f, 0, 0.10f, 0.30f)),
        ("tmax400", new Def("Kodak T-Max 400", Neutral, One, One, 0f, 0.03f, 0.18f, null, 0f, 0, 0.05f, 0.24f)),
        ("hp5", new Def("Ilford HP5 Plus 400", Neutral, One, One, 0f, 0.035f, 0.16f, null, 0f, 0, 0.08f, 0.28f)),
        ("fp4", new Def("Ilford FP4 Plus 125", Neutral, One, One, 0f, 0.03f, 0.14f, null, 0f, 0, 0.04f, 0.22f)),
        ("delta3200", new Def("Ilford Delta 3200", Neutral, One, One, 0f, 0.045f, 0.15f, null, 0f, 0, 0.16f, 0.34f)),
    };

    private static readonly ConcurrentDictionary<string, FilmStock> Cache = new();

    private static readonly string LutDir =
        Path.Combine(AppContext.BaseDirectory, "FilmProcessing", "Luts");

    public static IReadOnlyList<(string Id, string DisplayName)> Catalog { get; } =
        Defs.Select(d => (d.Id, d.Def.Name)).ToList();

    public static bool Exists(string id) => Defs.Any(d => d.Id == id);

    public static FilmStock Resolve(string? id)
    {
        id = string.IsNullOrWhiteSpace(id) || !Exists(id) ? Default : id;
        return Cache.GetOrAdd(id, Build);
    }

    private static FilmStock Build(string id)
    {
        var def = Defs.First(d => d.Id == id).Def;
        var chain = new List<IImageFilter>();

        var lutPath = Path.Combine(LutDir, $"{id}.cube");
        if (File.Exists(lutPath))
            chain.Add(LutFilter.Load(lutPath));
        else
            chain.Add(new ColorGradeFilter(def.Lift, def.Gamma, def.Gain, def.Sat));

        chain.Add(new ToneCurveFilter(def.BlackLift, def.Contrast));
        if (def.HalThreshold is { } threshold)
            chain.Add(new HalationFilter(threshold, def.HalIntensity, def.HalRadius));
        chain.Add(new GrainFilter(def.Grain, seed: StableSeed(id)));
        chain.Add(new VignetteFilter(def.Vignette));

        return new FilmStock(id, def.Name, chain);
    }

    // FNV-1a: stable across restarts, unlike string.GetHashCode().
    private static int StableSeed(string id)
    {
        uint hash = 2166136261;
        foreach (var c in id)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return (int)hash;
    }
}
