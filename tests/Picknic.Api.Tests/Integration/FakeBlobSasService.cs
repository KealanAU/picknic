using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Picknic.Api.Storage;

namespace Picknic.Api.Tests.Integration;

/// <summary>
/// In-memory stand-in for Azure blob storage. Tracks blob metadata only —
/// size, content type, existence — which is all the endpoints consult.
/// </summary>
public sealed class FakeBlobSasService() : BlobSasService(Options.Create(new StorageOptions()))
{
    private readonly ConcurrentDictionary<string, (long Size, string? ContentType)> _blobs = new();

    public override bool Enabled => true;
    public override long MaxBytes => 25 * 1024 * 1024;
    public override string Container => "photos";

    public override Task InitializeAsync() => Task.CompletedTask;

    public override Task<UploadTarget> CreateUploadSasAsync(Guid eventId, Guid guestId, DateTimeOffset expiresAt)
    {
        var blobPath = $"{eventId}/{guestId}/{Guid.NewGuid():n}.jpg";
        return Task.FromResult(new UploadTarget($"https://blobs.invalid/photos/{blobPath}?sig=test", blobPath));
    }

    public override Task<string> CreateReadSasAsync(string blobPath, DateTimeOffset expiresAt) =>
        Task.FromResult($"https://blobs.invalid/photos/{blobPath}?sig=test");

    public override Task<long?> GetSizeAsync(string blobPath) =>
        Task.FromResult(_blobs.TryGetValue(blobPath, out var blob) ? blob.Size : (long?)null);

    public override Task<(long Size, string? ContentType)?> GetBlobInfoAsync(string blobPath) =>
        Task.FromResult<(long Size, string? ContentType)?>(
            _blobs.TryGetValue(blobPath, out var blob) ? blob : null);

    public override Task DeleteAsync(string blobPath)
    {
        _blobs.TryRemove(blobPath, out _);
        return Task.CompletedTask;
    }

    public override Task<Stream?> OpenReadAsync(string blobPath) => Task.FromResult<Stream?>(null);

    public override Task UploadAsync(string blobPath, byte[] content, string contentType)
    {
        _blobs[blobPath] = (content.LongLength, contentType);
        return Task.CompletedTask;
    }

    public override Task<bool> ExistsAsync(string blobPath) =>
        Task.FromResult(_blobs.ContainsKey(blobPath));

    /// <summary>Simulates the guest's PUT to the SAS URL actually landing the blob.</summary>
    public void Land(string blobPath, long size = 1024, string? contentType = "image/jpeg") =>
        _blobs[blobPath] = (size, contentType);
}
