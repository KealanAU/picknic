namespace Picknic.Api.FilmProcessing;

public sealed class FilmProcessor(IImageCodec codec) : IFilmProcessor
{
    public byte[] Develop(Stream source, string? stockId = null, string? printStyleId = null, int quality = 90)
    {
        var stock = FilmStocks.Resolve(stockId);
        var buffer = codec.Decode(source);
        foreach (var filter in stock.Chain)
            filter.Apply(buffer);

        if (PrintStyles.Resolve(printStyleId) is { } print)
            buffer = PrintFramer.Apply(buffer, print);

        return codec.EncodeJpeg(buffer, quality);
    }
}
