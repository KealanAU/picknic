namespace Picknic.Api.FilmProcessing;

public interface IFilmProcessor
{
    byte[] Develop(Stream source, string? stockId = null, string? printStyleId = null, int quality = 90);
}
