namespace Picknic.Api.FilmProcessing;

/// <summary>
/// A decode-agnostic image: a flat RGBA buffer of linear-ish 0..1 floats.
///
/// This is the seam that keeps the "film look" decoupled from any image library.
/// Every <see cref="IImageFilter"/> operates purely on this struct — no JPEG, no
/// EXIF, no <c>SixLabors</c> types — so the grading maths is trivially unit-testable
/// and the codec (<see cref="IImageCodec"/>) is the only thing that ever needs to
/// change if we swap ImageSharp for SkiaSharp, GPU, etc.
/// </summary>
public sealed class PixelBuffer
{
    public int Width { get; }
    public int Height { get; }

    /// <summary>Row-major RGBA, 4 floats per pixel, channels in 0..1.</summary>
    public float[] Data { get; }

    public PixelBuffer(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Dimensions must be positive.");
        Width = width;
        Height = height;
        Data = new float[width * height * 4];
    }

    public int PixelCount => Width * Height;

    /// <summary>Index of the R channel of pixel (x, y); G/B/A follow at +1/+2/+3.</summary>
    public int Offset(int x, int y) => (y * Width + x) * 4;

    public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    /// <summary>Rec. 709 luma of a pixel — used by highlight/shadow-aware filters.</summary>
    public static float Luma(float r, float g, float b) => 0.2126f * r + 0.7152f * g + 0.0722f * b;
}
