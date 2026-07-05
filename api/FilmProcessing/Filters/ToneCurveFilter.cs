namespace Picknic.Api.FilmProcessing.Filters;

/// <summary>
/// The tonal signature of film: lifted (never-quite-black) shadows plus a gentle
/// S-curve for contrast. Applied to luma-preserving RGB equally.
/// </summary>
public sealed class ToneCurveFilter : IImageFilter
{
    private readonly float _blackLift;   // 0 = true black, ~0.05 = faded film black
    private readonly float _contrast;    // 0 = none, ~0.15 = noticeable S-curve

    public ToneCurveFilter(float blackLift = 0.04f, float contrast = 0.12f)
    {
        _blackLift = blackLift;
        _contrast = contrast;
    }

    public void Apply(PixelBuffer buffer)
    {
        var d = buffer.Data;
        for (var i = 0; i < d.Length; i += 4)
        {
            d[i] = Curve(d[i]);
            d[i + 1] = Curve(d[i + 1]);
            d[i + 2] = Curve(d[i + 2]);
        }
    }

    private float Curve(float v)
    {
        // Lift blacks: compress the range into [blackLift, 1].
        v = _blackLift + v * (1f - _blackLift);
        // S-curve around mid grey using a smoothstep-style push.
        if (_contrast != 0f)
        {
            var s = v * v * (3f - 2f * v); // smoothstep(0,1,v)
            v += (s - v) * _contrast;
        }
        return PixelBuffer.Clamp01(v);
    }
}
