namespace Picknic.Api.FilmProcessing.Filters;

/// <summary>
/// The colour character of a stock, without needing a LUT file: per-channel
/// lift / gamma / gain (a.k.a. shadows / midtones / highlights), a global
/// temperature/tint push, and a saturation scale.
///
/// This is enough to get recognisable "Portra-ish" / "Gold-ish" looks out of the
/// box. For exact film emulation, prefer a real 3D LUT via <see cref="LutFilter"/>
/// — a stock can use either or both.
/// </summary>
public sealed class ColorGradeFilter : IImageFilter
{
    // lift shifts blacks, gain scales whites, gamma bends the midtones.
    private readonly (float R, float G, float B) _lift;
    private readonly (float R, float G, float B) _gamma;
    private readonly (float R, float G, float B) _gain;
    private readonly float _saturation;

    public ColorGradeFilter(
        (float R, float G, float B)? lift = null,
        (float R, float G, float B)? gamma = null,
        (float R, float G, float B)? gain = null,
        float saturation = 1f)
    {
        _lift = lift ?? (0f, 0f, 0f);
        _gamma = gamma ?? (1f, 1f, 1f);
        _gain = gain ?? (1f, 1f, 1f);
        _saturation = saturation;
    }

    public void Apply(PixelBuffer buffer)
    {
        var d = buffer.Data;
        for (var i = 0; i < d.Length; i += 4)
        {
            var r = Grade(d[i], _lift.R, _gamma.R, _gain.R);
            var g = Grade(d[i + 1], _lift.G, _gamma.G, _gain.G);
            var b = Grade(d[i + 2], _lift.B, _gamma.B, _gain.B);

            if (_saturation != 1f)
            {
                var l = PixelBuffer.Luma(r, g, b);
                r = l + (r - l) * _saturation;
                g = l + (g - l) * _saturation;
                b = l + (b - l) * _saturation;
            }

            d[i] = PixelBuffer.Clamp01(r);
            d[i + 1] = PixelBuffer.Clamp01(g);
            d[i + 2] = PixelBuffer.Clamp01(b);
            // alpha (d[i + 3]) untouched
        }
    }

    // lift/gamma/gain applied in the standard order: offset blacks, scale, then
    // curve the midtones.
    private static float Grade(float v, float lift, float gamma, float gain)
    {
        v = lift + v * (gain - lift);
        if (v > 0f && gamma != 1f)
            v = MathF.Pow(v, 1f / gamma);
        return v;
    }
}
