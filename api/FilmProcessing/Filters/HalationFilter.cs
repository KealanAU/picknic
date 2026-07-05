using System.Numerics;

namespace Picknic.Api.FilmProcessing.Filters;

/// <summary>
/// Halation: the red-orange glow that bleeds around bright areas on film (the
/// Cinestill signature). We isolate the highlights, blur them, tint the glow,
/// and screen-blend it back over the image.
///
/// The blur is a self-contained separable box blur so this filter — like the
/// rest of the look — carries no image-library dependency.
/// </summary>
public sealed class HalationFilter : IImageFilter
{
    private readonly float _threshold;   // luma above which pixels glow
    private readonly float _intensity;   // strength of the added glow
    private readonly int _radius;        // blur radius in pixels (spread of the glow)
    private readonly Vector3 _tint;      // glow colour (warm red-orange)

    public HalationFilter(
        float threshold = 0.72f,
        float intensity = 0.5f,
        int radius = 12,
        Vector3? tint = null)
    {
        _threshold = threshold;
        _intensity = intensity;
        _radius = Math.Max(1, radius);
        _tint = tint ?? new Vector3(1.0f, 0.45f, 0.25f);
    }

    public void Apply(PixelBuffer buffer)
    {
        if (_intensity <= 0f) return;
        var n = buffer.PixelCount;

        // 1. Highlight mask, tinted: how much each pixel contributes to the glow.
        var glow = new float[n * 3];
        var d = buffer.Data;
        for (int i = 0, p = 0; i < d.Length; i += 4, p += 3)
        {
            var l = PixelBuffer.Luma(d[i], d[i + 1], d[i + 2]);
            var m = l <= _threshold ? 0f : (l - _threshold) / (1f - _threshold);
            glow[p] = m * _tint.X;
            glow[p + 1] = m * _tint.Y;
            glow[p + 2] = m * _tint.Z;
        }

        // 2. Spread it.
        BoxBlur(glow, buffer.Width, buffer.Height, _radius);

        // 3. Screen-blend the glow back: result = 1 - (1-base)(1-glow).
        for (int i = 0, p = 0; i < d.Length; i += 4, p += 3)
        {
            d[i] = Screen(d[i], glow[p] * _intensity);
            d[i + 1] = Screen(d[i + 1], glow[p + 1] * _intensity);
            d[i + 2] = Screen(d[i + 2], glow[p + 2] * _intensity);
        }
    }

    private static float Screen(float a, float b)
    {
        b = PixelBuffer.Clamp01(b);
        return PixelBuffer.Clamp01(1f - (1f - a) * (1f - b));
    }

    // Separable box blur over an interleaved RGB float buffer, two passes to
    // approximate a Gaussian. Operates in place via a scratch copy per axis.
    private static void BoxBlur(float[] rgb, int w, int h, int radius)
    {
        for (var pass = 0; pass < 2; pass++)
        {
            BlurHorizontal(rgb, w, h, radius);
            BlurVertical(rgb, w, h, radius);
        }
    }

    private static void BlurHorizontal(float[] rgb, int w, int h, int radius)
    {
        var line = new float[w * 3];
        var window = 2 * radius + 1;
        for (var y = 0; y < h; y++)
        {
            var row = y * w * 3;
            Array.Copy(rgb, row, line, 0, w * 3);
            for (var x = 0; x < w; x++)
            {
                float r = 0, g = 0, b = 0;
                for (var k = -radius; k <= radius; k++)
                {
                    var xx = Math.Clamp(x + k, 0, w - 1) * 3;
                    r += line[xx];
                    g += line[xx + 1];
                    b += line[xx + 2];
                }
                var o = row + x * 3;
                rgb[o] = r / window;
                rgb[o + 1] = g / window;
                rgb[o + 2] = b / window;
            }
        }
    }

    private static void BlurVertical(float[] rgb, int w, int h, int radius)
    {
        var col = new float[h * 3];
        var window = 2 * radius + 1;
        for (var x = 0; x < w; x++)
        {
            for (var y = 0; y < h; y++)
            {
                var src = (y * w + x) * 3;
                col[y * 3] = rgb[src];
                col[y * 3 + 1] = rgb[src + 1];
                col[y * 3 + 2] = rgb[src + 2];
            }
            for (var y = 0; y < h; y++)
            {
                float r = 0, g = 0, b = 0;
                for (var k = -radius; k <= radius; k++)
                {
                    var yy = Math.Clamp(y + k, 0, h - 1) * 3;
                    r += col[yy];
                    g += col[yy + 1];
                    b += col[yy + 2];
                }
                var o = (y * w + x) * 3;
                rgb[o] = r / window;
                rgb[o + 1] = g / window;
                rgb[o + 2] = b / window;
            }
        }
    }
}
