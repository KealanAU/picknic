using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Picknic.Api.FilmProcessing;

namespace Picknic.Api.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>e.g. https://picknicdev.blob.core.windows.net — used with managed identity in prod.</summary>
    public string AccountUrl { get; set; } = string.Empty;

    /// <summary>
    /// Account-key connection string (Azurite / local dev). When set, the service
    /// signs account-key SAS instead of Entra ID user-delegation SAS.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional host to rewrite generated SAS URLs to (e.g. http://localhost:10000/devstoreaccount1),
    /// so a browser can reach Azurite even though the API talks to it as "azurite".
    /// </summary>
    public string PublicEndpoint { get; set; } = string.Empty;

    public string Container { get; set; } = "photos";

    /// <summary>Max accepted photo size; enforced on the complete callback.</summary>
    public long MaxBytes { get; set; } = 25 * 1024 * 1024;

    public bool Enabled =>
        !string.IsNullOrWhiteSpace(AccountUrl) || !string.IsNullOrWhiteSpace(ConnectionString);
}

public record UploadTarget(string UploadUrl, string BlobPath);

/// <summary>
/// Mints SAS for photo blobs. In production it uses Entra ID / managed-identity
/// user-delegation SAS (no account key). When a connection string is configured
/// (Azurite / local dev), it falls back to account-key SAS, since the emulator
/// doesn't support user delegation. Upload SAS is create/write-only on a single,
/// server-named blob and expires at the upload window close.
/// Members are virtual so integration tests can substitute an in-memory fake.
/// </summary>
public class BlobSasService(IOptions<StorageOptions> options)
{
    private readonly StorageOptions _opts = options.Value;
    private readonly bool _useConnectionString = !string.IsNullOrWhiteSpace(options.Value.ConnectionString);

    public virtual bool Enabled => _opts.Enabled;

    public virtual long MaxBytes => _opts.MaxBytes;

    private BlobServiceClient Service() => _useConnectionString
        ? new BlobServiceClient(_opts.ConnectionString)
        : new BlobServiceClient(new Uri(_opts.AccountUrl), new DefaultAzureCredential());

    /// <summary>The blob container photos live in — used to parse Event Grid subjects.</summary>
    public virtual string Container => _opts.Container;

    /// <summary>Creates the container if missing — used in dev/emulator where Terraform hasn't.</summary>
    public virtual async Task InitializeAsync()
    {
        if (!Enabled || !_useConnectionString) return;
        await Service().GetBlobContainerClient(_opts.Container).CreateIfNotExistsAsync();
    }

    public virtual async Task<UploadTarget> CreateUploadSasAsync(
        Guid eventId, Guid guestId, DateTimeOffset expiresAt)
    {
        // Guest id is in the path so an Event Grid BlobCreated handler can
        // attribute the photo without trusting a client callback.
        var blobPath = $"{eventId}/{guestId}/{Guid.NewGuid():n}.jpg";
        var url = await SignedUriAsync(blobPath,
            BlobSasPermissions.Create | BlobSasPermissions.Write, expiresAt);
        return new UploadTarget(url, blobPath);
    }

    public virtual Task<string> CreateReadSasAsync(string blobPath, DateTimeOffset expiresAt) =>
        SignedUriAsync(blobPath, BlobSasPermissions.Read, expiresAt);

    public virtual async Task<long?> GetSizeAsync(string blobPath)
    {
        var blob = Service().GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        if (!await blob.ExistsAsync()) return null;
        var props = await blob.GetPropertiesAsync();
        return props.Value.ContentLength;
    }

    public virtual async Task<(long Size, string? ContentType)?> GetBlobInfoAsync(string blobPath)
    {
        var blob = Service().GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        if (!await blob.ExistsAsync()) return null;
        var props = await blob.GetPropertiesAsync();
        return (props.Value.ContentLength, props.Value.ContentType);
    }

    public virtual async Task DeleteAsync(string blobPath)
    {
        var blob = Service().GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        await blob.DeleteIfExistsAsync();
    }

    /// <summary>Deletes a photo's original blob and its developed derivative together.</summary>
    public virtual async Task DeleteWithDerivativeAsync(string blobPath)
    {
        await DeleteAsync(blobPath);
        await DeleteAsync(FilmDeveloper.DevelopedPath(blobPath));
    }

    /// <summary>Opens a blob for reading server-side (e.g. to develop a photo). Null if absent.</summary>
    public virtual async Task<Stream?> OpenReadAsync(string blobPath)
    {
        var blob = Service().GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        if (!await blob.ExistsAsync()) return null;
        return await blob.OpenReadAsync();
    }

    /// <summary>Writes bytes to a blob server-side, overwriting, with the given content type.</summary>
    public virtual async Task UploadAsync(string blobPath, byte[] content, string contentType)
    {
        var blob = Service().GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        using var ms = new MemoryStream(content);
        await blob.UploadAsync(ms, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        });
    }

    public virtual async Task<bool> ExistsAsync(string blobPath)
    {
        var blob = Service().GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        return await blob.ExistsAsync();
    }

    private async Task<string> SignedUriAsync(
        string blobPath, BlobSasPermissions perms, DateTimeOffset expiresAt)
    {
        var service = Service();
        var blob = service.GetBlobContainerClient(_opts.Container).GetBlobClient(blobPath);
        var now = DateTimeOffset.UtcNow;

        var builder = new BlobSasBuilder
        {
            BlobContainerName = _opts.Container,
            BlobName = blobPath,
            Resource = "b",
            StartsOn = now.AddMinutes(-2),
            ExpiresOn = expiresAt,
            // Azurite serves http; production is https-only.
            Protocol = _useConnectionString ? SasProtocol.HttpsAndHttp : SasProtocol.Https,
        };
        builder.SetPermissions(perms);

        string url;
        if (_useConnectionString)
        {
            // Account-key SAS — the connection-string client carries the shared key.
            url = blob.GenerateSasUri(builder).ToString();
        }
        else
        {
            var key = await DelegationKeyAsync(service, expiresAt);
            var sas = builder.ToSasQueryParameters(key, service.AccountName).ToString();
            url = $"{blob.Uri}?{sas}";
        }

        return RewriteHost(service, url);
    }

    // One delegation key covers many SAS mints (GET /photos signs one per photo);
    // fetching it per blob is a storage round-trip each. Cached until it can no
    // longer cover a requested expiry.
    private static UserDelegationKey? _delegationKey;
    private static readonly SemaphoreSlim DelegationKeyLock = new(1, 1);

    private static async Task<UserDelegationKey> DelegationKeyAsync(
        BlobServiceClient service, DateTimeOffset expiresAt)
    {
        if (_delegationKey is { } cached && cached.SignedExpiresOn >= expiresAt) return cached;
        await DelegationKeyLock.WaitAsync();
        try
        {
            if (_delegationKey is { } fresh && fresh.SignedExpiresOn >= expiresAt) return fresh;
            var now = DateTimeOffset.UtcNow;
            var expiresOn = expiresAt > now.AddHours(2) ? expiresAt : now.AddHours(2);
            var key = await service.GetUserDelegationKeyAsync(
                startsOn: now.AddMinutes(-2), expiresOn: expiresOn, cancellationToken: default);
            _delegationKey = key.Value;
            return key.Value;
        }
        finally
        {
            DelegationKeyLock.Release();
        }
    }

    // The SAS signature covers the account + path, not the host — so we can swap
    // the internal Azurite host ("azurite") for a browser-reachable one ("localhost").
    private string RewriteHost(BlobServiceClient service, string url)
    {
        if (string.IsNullOrWhiteSpace(_opts.PublicEndpoint)) return url;
        var internalBase = service.Uri.ToString().TrimEnd('/');
        var publicBase = _opts.PublicEndpoint.TrimEnd('/');
        return url.StartsWith(internalBase, StringComparison.OrdinalIgnoreCase)
            ? string.Concat(publicBase, url.AsSpan(internalBase.Length))
            : url;
    }
}
