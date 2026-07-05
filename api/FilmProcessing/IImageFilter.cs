namespace Picknic.Api.FilmProcessing;

/// <summary>
/// One stage of a film look. Filters are small, order-dependent, and composed
/// into a chain by a <see cref="FilmStock"/>. Each mutates the buffer in place.
///
/// Keep filters pure (no I/O, no shared state) so a stock is just data + a list
/// of these, and any filter can be tested in isolation on a hand-built buffer.
/// </summary>
public interface IImageFilter
{
    void Apply(PixelBuffer buffer);
}
