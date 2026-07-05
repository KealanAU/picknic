namespace Picknic.Api.FilmProcessing;

/// <summary>
/// Composites a developed photo into an instant-print card: centre-crop to the
/// window aspect, then pad with paper borders. Pure <see cref="PixelBuffer"/> work
/// — no resampling (the crop stays at native resolution, borders are added around
/// it), so it carries no image-library dependency, same as the filters.
/// </summary>
public static class PrintFramer
{
    public static PixelBuffer Apply(PixelBuffer photo, PrintStyle style)
    {
        // 1. Centre-crop the photo to the window aspect.
        var (cropW, cropH, cropX, cropY) = CropToAspect(photo.Width, photo.Height, style.PhotoAspect);

        // 2. Border widths, scaled from the crop so prints look the same at any size.
        var side = Math.Max(1, (int)MathF.Round(cropW * style.SideBorder));
        var top = Math.Max(1, (int)MathF.Round(cropW * style.TopBorder));
        var bottom = Math.Max(1, (int)MathF.Round(cropW * style.BottomBorder));

        var cardW = cropW + 2 * side;
        var cardH = cropH + top + bottom;
        var card = new PixelBuffer(cardW, cardH);

        // 3. Fill with paper.
        var d = card.Data;
        var (pr, pg, pb) = style.Paper;
        for (var i = 0; i < d.Length; i += 4)
        {
            d[i] = pr;
            d[i + 1] = pg;
            d[i + 2] = pb;
            d[i + 3] = 1f;
        }

        // 4. Blit the cropped photo into the window at (side, top).
        var src = photo.Data;
        for (var y = 0; y < cropH; y++)
        {
            var srcRow = photo.Offset(cropX, cropY + y);
            var dstRow = card.Offset(side, top + y);
            Array.Copy(src, srcRow, d, dstRow, cropW * 4);
        }

        return card;
    }

    // Largest centred rectangle of the given aspect (w/h) that fits in src.
    private static (int W, int H, int X, int Y) CropToAspect(int srcW, int srcH, float aspect)
    {
        var srcAspect = (float)srcW / srcH;
        int w, h;
        if (srcAspect > aspect)
        {
            // Source too wide — trim the sides.
            h = srcH;
            w = (int)MathF.Round(srcH * aspect);
        }
        else
        {
            // Source too tall — trim top/bottom.
            w = srcW;
            h = (int)MathF.Round(srcW / aspect);
        }
        w = Math.Clamp(w, 1, srcW);
        h = Math.Clamp(h, 1, srcH);
        return (w, h, (srcW - w) / 2, (srcH - h) / 2);
    }
}
