using SkiaSharp;

namespace Picknic.Api.FilmProcessing;

/// <summary>
/// SkiaSharp-backed <see cref="IImageCodec"/> — decode/encode only; it never
/// touches the film look. SkiaSharp is MIT-licensed (free for commercial use)
/// and actively patched, which is why we use it over ImageSharp's 2.x line
/// (unpatched advisories) or 3.x line (commercial licence). Linux containers get
/// the native binaries from the <c>SkiaSharp.NativeAssets.Linux.NoDependencies</c>
/// package referenced in the csproj.
/// </summary>
public sealed class SkiaImageCodec : IImageCodec
{
    public PixelBuffer Decode(Stream source)
    {
        using var data = SKData.Create(source);
        using var codec = SKCodec.Create(data)
            ?? throw new InvalidDataException("Unsupported or corrupt image.");

        using var decoded = SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("Could not decode image.");

        // Camera photos carry EXIF orientation; bake it in so the developed JPEG
        // is upright without relying on the viewer honouring EXIF.
        using var upright = Orient(decoded, codec.EncodedOrigin);

        var w = upright.Width;
        var h = upright.Height;
        var buffer = new PixelBuffer(w, h);
        var dst = buffer.Data;
        var src = upright.GetPixelSpan(); // RGBA8888, tightly packed

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
            bytes[s + 3] = 255; // JPEG is opaque
        }

        var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var image = SKImage.FromPixelCopy(info, bytes);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, Math.Clamp(quality, 1, 100));
        return encoded.ToArray();
    }

    private static byte ToByte(float v) => (byte)(PixelBuffer.Clamp01(v) * 255f + 0.5f);

    // Returns an upright RGBA8888 copy for the given EXIF origin. The four
    // transpose/rotate origins swap width and height.
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

        // Affine map from source pixel coords to the upright destination.
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

    // X' = sx·x + kx·y + tx ; Y' = ky·x + sy·y + ty
    private static SKMatrix Matrix(float sx, float kx, float tx, float ky, float sy, float ty) =>
        new()
        {
            ScaleX = sx, SkewX = kx, TransX = tx,
            SkewY = ky, ScaleY = sy, TransY = ty,
            Persp0 = 0, Persp1 = 0, Persp2 = 1,
        };
}
