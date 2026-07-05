namespace Picknic.Api.FilmProcessing;

/// <summary>
/// Develops a raw photo into a film-look JPEG. Orchestrates codec + filter chain;
/// knows nothing about blobs, HTTP, or the database.
/// </summary>
public interface IFilmProcessor
{
    /// <summary>
    /// Applies the named stock to an encoded image and optionally frames it as an
    /// instant print, returning JPEG bytes.
    /// </summary>
    byte[] Develop(Stream source, string? stockId = null, string? printStyleId = null, int quality = 90);
}
