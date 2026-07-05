namespace Picknic.Api.FilmProcessing.Filters;

// Lifted (never-quite-black) shadows plus a gentle S-curve for contrast.
public sealed class ToneCurveFilter : IImageFilter
{
    private readonly float _blackLift;
    private readonly float _contrast;

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
        v = _blackLift + v * (1f - _blackLift);
        if (_contrast != 0f)
        {
            var s = v * v * (3f - 2f * v);
            v += (s - v) * _contrast;
        }
        return PixelBuffer.Clamp01(v);
    }
}
