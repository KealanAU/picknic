using Picknic.Api.Storage;

namespace Picknic.Api.FilmProcessing;

// Reads the raw upload, develops it, and writes the derivative alongside the
// original ({id}.jpg -> {id}.dev.jpg) so developing never mutates the original.
public sealed class FilmDeveloper(IFilmProcessor processor, BlobSasService blobs)
{
    private const string DevSuffix = ".dev.jpg";

    public static string DevelopedPath(string originalBlobPath)
    {
        var dot = originalBlobPath.LastIndexOf('.');
        var stem = dot < 0 ? originalBlobPath : originalBlobPath[..dot];
        return stem + DevSuffix;
    }

    public static bool IsDeveloped(string blobPath) =>
        blobPath.EndsWith(DevSuffix, StringComparison.OrdinalIgnoreCase);

    public async Task<string?> DevelopAsync(
        string originalBlobPath, string? stockId, string? printStyleId = null,
        CancellationToken ct = default)
    {
        if (!blobs.Enabled || IsDeveloped(originalBlobPath)) return null;

        await using var source = await blobs.OpenReadAsync(originalBlobPath);
        if (source is null) return null;

        // ImageSharp needs a seekable stream, so buffer the network read to memory.
        using var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct);
        ms.Position = 0;

        var developed = processor.Develop(ms, stockId, printStyleId);
        var devPath = DevelopedPath(originalBlobPath);
        await blobs.UploadAsync(devPath, developed, "image/jpeg");
        return devPath;
    }
}
