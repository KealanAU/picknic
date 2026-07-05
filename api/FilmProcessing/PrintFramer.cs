namespace Picknic.Api.FilmProcessing;

// Centre-crops a developed photo to the window aspect and pads it with paper
// borders to make an instant-print card. No resampling.
public static class PrintFramer
{
    public static PixelBuffer Apply(PixelBuffer photo, PrintStyle style)
    {
        var (cropW, cropH, cropX, cropY) = CropToAspect(photo.Width, photo.Height, style.PhotoAspect);

        // Borders scale from the crop so prints look the same at any size.
        var side = Math.Max(1, (int)MathF.Round(cropW * style.SideBorder));
        var top = Math.Max(1, (int)MathF.Round(cropW * style.TopBorder));
        var bottom = Math.Max(1, (int)MathF.Round(cropW * style.BottomBorder));

        var cardW = cropW + 2 * side;
        var cardH = cropH + top + bottom;
        var card = new PixelBuffer(cardW, cardH);

        var d = card.Data;
        var (pr, pg, pb) = style.Paper;
        for (var i = 0; i < d.Length; i += 4)
        {
            d[i] = pr;
            d[i + 1] = pg;
            d[i + 2] = pb;
            d[i + 3] = 1f;
        }

        var src = photo.Data;
        for (var y = 0; y < cropH; y++)
        {
            var srcRow = photo.Offset(cropX, cropY + y);
            var dstRow = card.Offset(side, top + y);
            Array.Copy(src, srcRow, d, dstRow, cropW * 4);
        }

        return card;
    }

    private static (int W, int H, int X, int Y) CropToAspect(int srcW, int srcH, float aspect)
    {
        var srcAspect = (float)srcW / srcH;
        int w, h;
        if (srcAspect > aspect)
        {
            h = srcH;
            w = (int)MathF.Round(srcH * aspect);
        }
        else
        {
            w = srcW;
            h = (int)MathF.Round(srcW / aspect);
        }
        w = Math.Clamp(w, 1, srcW);
        h = Math.Clamp(h, 1, srcH);
        return (w, h, (srcW - w) / 2, (srcH - h) / 2);
    }
}
