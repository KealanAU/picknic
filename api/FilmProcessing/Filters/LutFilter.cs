using System.Globalization;
using System.Numerics;

namespace Picknic.Api.FilmProcessing.Filters;

/// <summary>
/// Applies a 3D colour LUT (Adobe/Resolve <c>.cube</c>) with trilinear
/// interpolation — the industry-standard way to emulate a specific film stock.
///
/// Drop <c>portra400.cube</c>, <c>cinestill800t.cube</c>, etc. into
/// <c>FilmProcessing/Luts/</c> and point a <see cref="FilmStock"/> at it. This
/// filter is optional: stocks without a LUT fall back to <see cref="ColorGradeFilter"/>.
/// </summary>
public sealed class LutFilter : IImageFilter
{
    private readonly int _size;
    private readonly Vector3[] _table; // size^3 entries, index = r + g*size + b*size*size

    private LutFilter(int size, Vector3[] table)
    {
        _size = size;
        _table = table;
    }

    public void Apply(PixelBuffer buffer)
    {
        var d = buffer.Data;
        for (var i = 0; i < d.Length; i += 4)
        {
            var c = Sample(d[i], d[i + 1], d[i + 2]);
            d[i] = PixelBuffer.Clamp01(c.X);
            d[i + 1] = PixelBuffer.Clamp01(c.Y);
            d[i + 2] = PixelBuffer.Clamp01(c.Z);
        }
    }

    private Vector3 Sample(float r, float g, float b)
    {
        var max = _size - 1;
        float fr = PixelBuffer.Clamp01(r) * max;
        float fg = PixelBuffer.Clamp01(g) * max;
        float fb = PixelBuffer.Clamp01(b) * max;

        int r0 = (int)fr, g0 = (int)fg, b0 = (int)fb;
        int r1 = Math.Min(r0 + 1, max), g1 = Math.Min(g0 + 1, max), b1 = Math.Min(b0 + 1, max);
        float dr = fr - r0, dg = fg - g0, db = fb - b0;

        // Trilinear blend of the 8 surrounding lattice points.
        Vector3 c000 = At(r0, g0, b0), c100 = At(r1, g0, b0);
        Vector3 c010 = At(r0, g1, b0), c110 = At(r1, g1, b0);
        Vector3 c001 = At(r0, g0, b1), c101 = At(r1, g0, b1);
        Vector3 c011 = At(r0, g1, b1), c111 = At(r1, g1, b1);

        var c00 = Vector3.Lerp(c000, c100, dr);
        var c10 = Vector3.Lerp(c010, c110, dr);
        var c01 = Vector3.Lerp(c001, c101, dr);
        var c11 = Vector3.Lerp(c011, c111, dr);
        var c0 = Vector3.Lerp(c00, c10, dg);
        var c1 = Vector3.Lerp(c01, c11, dg);
        return Vector3.Lerp(c0, c1, db);
    }

    private Vector3 At(int r, int g, int b) => _table[r + g * _size + b * _size * _size];

    /// <summary>Parses a <c>.cube</c> file. Supports 3D LUTs (LUT_3D_SIZE).</summary>
    public static LutFilter Load(string path)
    {
        using var reader = new StreamReader(path);
        return Parse(reader);
    }

    public static LutFilter Parse(TextReader reader)
    {
        int size = 0;
        Vector3[]? table = null;
        var idx = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            if (line.StartsWith("LUT_3D_SIZE", StringComparison.OrdinalIgnoreCase))
            {
                size = int.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1],
                    CultureInfo.InvariantCulture);
                table = new Vector3[size * size * size];
                continue;
            }
            // Skip metadata / 1D directives we don't model.
            if (char.IsLetter(line[0])) continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || table is null) continue;
            table[idx++] = new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        if (table is null || size == 0)
            throw new InvalidDataException("Not a valid 3D .cube LUT (missing LUT_3D_SIZE).");
        if (idx != table.Length)
            throw new InvalidDataException($"LUT entry count {idx} != expected {table.Length}.");
        return new LutFilter(size, table);
    }
}
