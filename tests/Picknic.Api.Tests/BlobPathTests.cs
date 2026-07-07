using Picknic.Api.Endpoints;

namespace Picknic.Api.Tests;

public class BlobPathTests
{
    private static readonly Guid Event = Guid.NewGuid();
    private static readonly Guid Guest = Guid.NewGuid();

    [Fact]
    public void Accepts_a_freshly_minted_path()
    {
        var path = $"{Event}/{Guest}/{Guid.NewGuid():n}.jpg";
        Assert.True(UploadEndpoints.IsMintedBlobPath(path, Event, Guest));
    }

    [Fact]
    public void Rejects_path_traversal()
    {
        var path = $"{Event}/{Guest}/../../{Guid.NewGuid()}/{Guid.NewGuid()}/x.jpg";
        Assert.False(UploadEndpoints.IsMintedBlobPath(path, Event, Guest));
    }

    [Fact]
    public void Rejects_another_guests_prefix()
    {
        var path = $"{Event}/{Guid.NewGuid()}/{Guid.NewGuid():n}.jpg";
        Assert.False(UploadEndpoints.IsMintedBlobPath(path, Event, Guest));
    }

    [Fact]
    public void Rejects_the_developed_derivative()
    {
        var path = $"{Event}/{Guest}/{Guid.NewGuid():n}.dev.jpg";
        Assert.False(UploadEndpoints.IsMintedBlobPath(path, Event, Guest));
    }

    [Fact]
    public void Rejects_extra_path_segments()
    {
        var path = $"{Event}/{Guest}/nested/{Guid.NewGuid():n}.jpg";
        Assert.False(UploadEndpoints.IsMintedBlobPath(path, Event, Guest));
    }

    [Fact]
    public void Rejects_a_non_guid_name()
    {
        Assert.False(UploadEndpoints.IsMintedBlobPath($"{Event}/{Guest}/notaguid.jpg", Event, Guest));
    }
}
