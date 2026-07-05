namespace Picknic.Api.FilmProcessing.Filters;

/// <summary>
/// Film grain: monochrome noise, weighted toward the midtones (grain is least
/// visible in deep shadows and blown highlights). Deterministic per-seed so a
/// given photo always develops the same way.
/// </summary>
public sealed class GrainFilter : IImageFilter
{
    private readonly float _amount;  // ~0.02 subtle … 0.12 heavy
    private readonly int _seed;

    public GrainFilter(float amount = 0.05f, int seed = 1)
    {
        _amount = amount;
        _seed = seed;
    }

    public void Apply(PixelBuffer buffer)
    {
        if (_amount <= 0f) return;
        var rng = new Random(_seed);
        var d = buffer.Data;
        for (var i = 0; i < d.Length; i += 4)
        {
            var l = PixelBuffer.Luma(d[i], d[i + 1], d[i + 2]);
            // Triangular midtone weighting: peak at 0.5, zero at the extremes.
            var weight = 1f - MathF.Abs(l - 0.5f) * 2f;
            // Gaussian-ish noise from two uniforms, in roughly [-1, 1].
            var n = (float)(rng.NextDouble() + rng.NextDouble() - 1.0);
            var delta = n * _amount * weight;

            d[i] = PixelBuffer.Clamp01(d[i] + delta);
            d[i + 1] = PixelBuffer.Clamp01(d[i + 1] + delta);
            d[i + 2] = PixelBuffer.Clamp01(d[i + 2] + delta);
        }
    }
}
