namespace Picknic.Api.FilmProcessing;

// Decode-agnostic image: a flat row-major RGBA buffer of 0..1 floats.
public sealed class PixelBuffer
{
    public int Width { get; }
    public int Height { get; }
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

    public int Offset(int x, int y) => (y * Width + x) * 4;

    public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

    public static float Luma(float r, float g, float b) => 0.2126f * r + 0.7152f * g + 0.0722f * b;
}
