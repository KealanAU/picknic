using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace Picknic.Api.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>e.g. https://picknicdev.blob.core.windows.net — empty disables storage.</summary>
    public string AccountUrl { get; set; } = string.Empty;
    public string Container { get; set; } = "photos";

    /// <summary>Max accepted photo size; enforced on the complete callback.</summary>
    public long MaxBytes { get; set; } = 25 * 1024 * 1024;

    public bool Enabled => !string.IsNullOrWhiteSpace(AccountUrl);
}

public record UploadTarget(string UploadUrl, string BlobPath);

/// <summary>
/// Mints user-delegation SAS (signed via Entra ID / managed identity, no
/// account key). Upload SAS is create/write-only on a single, server-named
/// blob and expires at the upload window close — so a leaked SAS can't read the
/// gallery, overwrite others, or outlive the deadline.
/// </summary>
public class BlobSasService(IOptions<StorageOptions> options)
{
    private readonly StorageOptions _opts = options.Value;

    public bool Enabled => _opts.Enabled;

    private BlobServiceClient Service() =>
        new(new Uri(_opts.AccountUrl), new DefaultAzureCredential());

    public async Task<UploadTarget> CreateUploadSasAsync(
        Guid eventId, DateTimeOffset expiresAt)
    {
        var blobPath = $"{eventId}/{Guid.NewGuid():n}.jpg";
        var service = Service();
        var blob = service.GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);

        var sas = await BuildSasAsync(service, blobPath,
            BlobSasPermissions.Create | BlobSasPermissions.Write, expiresAt);

        return new UploadTarget($"{blob.Uri}?{sas}", blobPath);
    }

    public async Task<string> CreateReadSasAsync(string blobPath, DateTimeOffset expiresAt)
    {
        var service = Service();
        var blob = service.GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        var sas = await BuildSasAsync(service, blobPath, BlobSasPermissions.Read, expiresAt);
        return $"{blob.Uri}?{sas}";
    }

    public async Task<long?> GetSizeAsync(string blobPath)
    {
        var blob = Service().GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        if (!await blob.ExistsAsync()) return null;
        var props = await blob.GetPropertiesAsync();
        return props.Value.ContentLength;
    }

    private async Task<string> BuildSasAsync(
        BlobServiceClient service, string blobPath,
        BlobSasPermissions perms, DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;
        var key = await service.GetUserDelegationKeyAsync(
            startsOn: now.AddMinutes(-2), expiresOn: expiresAt, cancellationToken: default);

        var builder = new BlobSasBuilder
        {
            BlobContainerName = _opts.Container,
            BlobName = blobPath,
            Resource = "b",
            StartsOn = now.AddMinutes(-2),
            ExpiresOn = expiresAt,
            Protocol = SasProtocol.Https,
        };
        builder.SetPermissions(perms);

        return builder
            .ToSasQueryParameters(key.Value, service.AccountName)
            .ToString();
    }
}
