namespace Picknic.Api.FilmProcessing.Filters;

// Monochrome noise weighted toward the midtones. Deterministic per seed.
public sealed class GrainFilter : IImageFilter
{
    private readonly float _amount;
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
            var weight = 1f - MathF.Abs(l - 0.5f) * 2f;
            var n = (float)(rng.NextDouble() + rng.NextDouble() - 1.0);
            var delta = n * _amount * weight;

            d[i] = PixelBuffer.Clamp01(d[i] + delta);
            d[i + 1] = PixelBuffer.Clamp01(d[i + 1] + delta);
            d[i + 2] = PixelBuffer.Clamp01(d[i + 2] + delta);
        }
    }
}
