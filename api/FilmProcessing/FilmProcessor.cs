namespace Picknic.Api.FilmProcessing;

/// <summary>
/// Decode → run the stock's filter chain → encode. The whole "look" is the
/// composition of small filters resolved from <see cref="FilmStocks"/>; this
/// class just sequences the three phases.
/// </summary>
public sealed class FilmProcessor(IImageCodec codec) : IFilmProcessor
{
    public byte[] Develop(Stream source, string? stockId = null, string? printStyleId = null, int quality = 90)
    {
        var stock = FilmStocks.Resolve(stockId);
        var buffer = codec.Decode(source);
        foreach (var filter in stock.Chain)
            filter.Apply(buffer);

        // Optional instant-print frame, after the look so borders stay clean.
        if (PrintStyles.Resolve(printStyleId) is { } print)
            buffer = PrintFramer.Apply(buffer, print);

        return codec.EncodeJpeg(buffer, quality);
    }
}
