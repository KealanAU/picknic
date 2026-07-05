namespace Picknic.Api.FilmProcessing;

public interface IImageCodec
{
    PixelBuffer Decode(Stream source);

    byte[] EncodeJpeg(PixelBuffer buffer, int quality = 90);
}
