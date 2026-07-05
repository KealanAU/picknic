using Picknic.Api.Storage;

namespace Picknic.Api.FilmProcessing;

/// <summary>
/// Bridges the pure <see cref="IFilmProcessor"/> to blob storage: read the raw
/// upload, develop it, and write the developed derivative alongside the original.
///
/// Derivatives use a naming convention rather than a new DB column: the developed
/// copy of <c>{event}/{guest}/{id}.jpg</c> is <c>{event}/{guest}/{id}.dev.jpg</c>.
/// The reveal read-path can serve the developed blob when it exists and fall back
/// to the original otherwise — so developing is a fully decoupled, re-runnable
/// step that never mutates the guest's original.
/// </summary>
public sealed class FilmDeveloper(IFilmProcessor processor, BlobSasService blobs)
{
    private const string DevSuffix = ".dev.jpg";

    /// <summary>The developed-derivative path for an original blob path.</summary>
    public static string DevelopedPath(string originalBlobPath)
    {
        var dot = originalBlobPath.LastIndexOf('.');
        var stem = dot < 0 ? originalBlobPath : originalBlobPath[..dot];
        return stem + DevSuffix;
    }

    public static bool IsDeveloped(string blobPath) =>
        blobPath.EndsWith(DevSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Develops the original at <paramref name="originalBlobPath"/> with the given
    /// stock and optional instant-print frame, and writes the derivative. Returns
    /// the derivative path, or null if the original is missing or storage is off.
    /// </summary>
    public async Task<string?> DevelopAsync(
        string originalBlobPath, string? stockId, string? printStyleId = null,
        CancellationToken ct = default)
    {
        if (!blobs.Enabled || IsDeveloped(originalBlobPath)) return null;

        await using var source = await blobs.OpenReadAsync(originalBlobPath);
        if (source is null) return null;

        // Buffer to memory: ImageSharp needs a seekable stream and the develop is
        // CPU-bound, so we don't want to hold the network stream open across it.
        using var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct);
        ms.Position = 0;

        var developed = processor.Develop(ms, stockId, printStyleId);
        var devPath = DevelopedPath(originalBlobPath);
        await blobs.UploadAsync(devPath, developed, "image/jpeg");
        return devPath;
    }
}
