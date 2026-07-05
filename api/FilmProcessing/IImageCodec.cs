namespace Picknic.Api.FilmProcessing;

/// <summary>
/// The one place that knows about image encoding. Decodes bytes into a
/// library-agnostic <see cref="PixelBuffer"/> and back to JPEG. Swapping the
/// image library (ImageSharp → SkiaSharp → GPU) means implementing only this.
/// </summary>
public interface IImageCodec
{
    /// <summary>Decodes an encoded image, applying EXIF orientation.</summary>
    PixelBuffer Decode(Stream source);

    /// <summary>Encodes a buffer to JPEG bytes at the given quality (1..100).</summary>
    byte[] EncodeJpeg(PixelBuffer buffer, int quality = 90);
}
