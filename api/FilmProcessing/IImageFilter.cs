namespace Picknic.Api.FilmProcessing;

public interface IImageFilter
{
    void Apply(PixelBuffer buffer);
}
