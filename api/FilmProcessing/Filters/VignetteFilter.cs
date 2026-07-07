namespace Picknic.Api.FilmProcessing.Filters;

public sealed class VignetteFilter : IImageFilter
{
    private readonly float _amount;
    private readonly float _feather;

    public VignetteFilter(float amount = 0.28f, float feather = 0.55f)
    {
        _amount = amount;
        _feather = feather;
    }

    public void Apply(PixelBuffer buffer)
    {
        if (_amount <= 0f) return;
        var cx = (buffer.Width - 1) / 2f;
        var cy = (buffer.Height - 1) / 2f;
        var maxDist = MathF.Sqrt(cx * cx + cy * cy);
        var d = buffer.Data;

        for (var y = 0; y < buffer.Height; y++)
        {
            for (var x = 0; x < buffer.Width; x++)
            {
                var dx = (x - cx) / maxDist;
                var dy = (y - cy) / maxDist;
                var dist = MathF.Sqrt(dx * dx + dy * dy);

                var t = (dist - _feather) / (1f - _feather);
                t = t < 0f ? 0f : t > 1f ? 1f : t;
                var factor = 1f - _amount * t * t;

                var o = buffer.Offset(x, y);
                d[o] *= factor;
                d[o + 1] *= factor;
                d[o + 2] *= factor;
            }
        }
    }
}
