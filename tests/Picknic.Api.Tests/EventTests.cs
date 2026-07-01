using Picknic.Api.Models;

namespace Picknic.Api.Tests;

public class EventTests
{
    private static Event NewEvent(DateTimeOffset opens, DateTimeOffset closes, DateTimeOffset reveal) => new()
    {
        Code = "ABC123",
        Name = "Test",
        JoinSecretEnc = "enc",
        HostId = "host",
        UploadOpensAt = opens,
        UploadClosesAt = closes,
        RevealAt = reveal,
    };

    private static readonly DateTimeOffset Opens = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Closes = new(2026, 1, 1, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Reveal = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UploadOpen_true_within_window()
    {
        var ev = NewEvent(Opens, Closes, Reveal);
        Assert.True(ev.UploadOpen(Opens.AddHours(1)));
    }

    [Fact]
    public void UploadOpen_true_at_open_boundary()
    {
        var ev = NewEvent(Opens, Closes, Reveal);
        Assert.True(ev.UploadOpen(Opens));
    }

    [Fact]
    public void UploadOpen_false_before_open()
    {
        var ev = NewEvent(Opens, Closes, Reveal);
        Assert.False(ev.UploadOpen(Opens.AddSeconds(-1)));
    }

    [Fact]
    public void UploadOpen_false_at_close_boundary()
    {
        var ev = NewEvent(Opens, Closes, Reveal);
        Assert.False(ev.UploadOpen(Closes));
    }

    [Fact]
    public void Revealed_false_before_reveal()
    {
        var ev = NewEvent(Opens, Closes, Reveal);
        Assert.False(ev.Revealed(Reveal.AddSeconds(-1)));
    }

    [Fact]
    public void Revealed_true_at_and_after_reveal()
    {
        var ev = NewEvent(Opens, Closes, Reveal);
        Assert.True(ev.Revealed(Reveal));
        Assert.True(ev.Revealed(Reveal.AddHours(1)));
    }
}
