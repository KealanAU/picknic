using SkiaSharp;

namespace Picknic.Api.FilmProcessing;

// SkiaSharp-backed codec: decode/encode only. Chosen over ImageSharp because it's
// MIT-licensed (free commercially) and actively patched; Linux natives come from
// the SkiaSharp.NativeAssets.Linux.NoDependencies package in the csproj.
public sealed class SkiaImageCodec : IImageCodec
{
    public PixelBuffer Decode(Stream source)
    {
        using var data = SKData.Create(source);
        using var codec = SKCodec.Create(data)
            ?? throw new InvalidDataException("Unsupported or corrupt image.");

        using var decoded = SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("Could not decode image.");

        // Bake in EXIF orientation so the developed JPEG is upright regardless of viewer.
        using var upright = Orient(decoded, codec.EncodedOrigin);

        var w = upright.Width;
        var h = upright.Height;
        var buffer = new PixelBuffer(w, h);
        var dst = buffer.Data;
        var src = upright.GetPixelSpan();

        for (var i = 0; i < w * h; i++)
        {
            var s = i * 4;
            dst[s] = src[s] / 255f;
            dst[s + 1] = src[s + 1] / 255f;
            dst[s + 2] = src[s + 2] / 255f;
            dst[s + 3] = src[s + 3] / 255f;
        }

        return buffer;
    }

    public byte[] EncodeJpeg(PixelBuffer buffer, int quality = 90)
    {
        var w = buffer.Width;
        var h = buffer.Height;
        var bytes = new byte[w * h * 4];
        var srcData = buffer.Data;
        for (var i = 0; i < w * h; i++)
        {
            var s = i * 4;
            bytes[s] = ToByte(srcData[s]);
            bytes[s + 1] = ToByte(srcData[s + 1]);
            bytes[s + 2] = ToByte(srcData[s + 2]);
            bytes[s + 3] = 255;
        }

        var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var image = SKImage.FromPixelCopy(info, bytes);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, Math.Clamp(quality, 1, 100));
        return encoded.ToArray();
    }

    private static byte ToByte(float v) => (byte)(PixelBuffer.Clamp01(v) * 255f + 0.5f);

    private static SKBitmap Orient(SKBitmap src, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
            return src.Copy(SKColorType.Rgba8888);

        var w = src.Width;
        var h = src.Height;
        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var dst = new SKBitmap(new SKImageInfo(
            swap ? h : w, swap ? w : h, SKColorType.Rgba8888, SKAlphaType.Unpremul));

        var m = origin switch
        {
            SKEncodedOrigin.TopRight => Matrix(-1, 0, w, 0, 1, 0),      // mirror X
            SKEncodedOrigin.BottomRight => Matrix(-1, 0, w, 0, -1, h),  // rotate 180
            SKEncodedOrigin.BottomLeft => Matrix(1, 0, 0, 0, -1, h),    // mirror Y
            SKEncodedOrigin.LeftTop => Matrix(0, 1, 0, 1, 0, 0),        // transpose
            SKEncodedOrigin.RightTop => Matrix(0, -1, h, 1, 0, 0),      // rotate 90 CW
            SKEncodedOrigin.RightBottom => Matrix(0, -1, h, -1, 0, w),  // transverse
            SKEncodedOrigin.LeftBottom => Matrix(0, 1, 0, -1, 0, w),    // rotate 90 CCW
            _ => SKMatrix.Identity,
        };

        using var canvas = new SKCanvas(dst);
        canvas.SetMatrix(m);
        canvas.DrawBitmap(src, 0, 0);
        canvas.Flush();
        return dst;
    }

    private static SKMatrix Matrix(float sx, float kx, float tx, float ky, float sy, float ty) =>
        new()
        {
            ScaleX = sx, SkewX = kx, TransX = tx,
            SkewY = ky, ScaleY = sy, TransY = ty,
            Persp0 = 0, Persp1 = 0, Persp2 = 1,
        };
}
