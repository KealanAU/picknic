using Microsoft.AspNetCore.DataProtection;
using Picknic.Api.Auth;
using Picknic.Api.Models;

namespace Picknic.Api.Tests;

public class JoinSecretProtectorTests
{
    [Fact]
    public void TryUnprotect_returns_false_for_invalid_payload()
    {
        var provider = DataProtectionProvider.Create("picknic-tests");
        var protector = new JoinSecretProtector(provider);

        var ok = protector.TryUnprotect("not-a-protected-secret", out var secret);

        Assert.False(ok);
        Assert.Equal(string.Empty, secret);
    }

    [Fact]
    public void TryUnprotect_returns_true_for_own_payload()
    {
        var provider = DataProtectionProvider.Create("picknic-tests");
        var protector = new JoinSecretProtector(provider);
        var encrypted = protector.Protect("join-secret");

        var ok = protector.TryUnprotect(encrypted, out var secret);

        Assert.True(ok);
        Assert.Equal("join-secret", secret);
    }

    [Fact]
    public void UnprotectOrRotate_keeps_readable_event_secret()
    {
        var provider = DataProtectionProvider.Create("picknic-tests");
        var protector = new JoinSecretProtector(provider);
        var encrypted = protector.Protect("join-secret");
        var ev = NewEvent(encrypted);

        var secret = protector.UnprotectOrRotate(ev, out var rotated);

        Assert.False(rotated);
        Assert.Equal("join-secret", secret);
        Assert.Equal(encrypted, ev.JoinSecretEnc);
    }

    [Fact]
    public void UnprotectOrRotate_replaces_unreadable_event_secret()
    {
        var provider = DataProtectionProvider.Create("picknic-tests");
        var protector = new JoinSecretProtector(provider);
        var ev = NewEvent("not-a-protected-secret");

        var secret = protector.UnprotectOrRotate(ev, out var rotated);

        Assert.True(rotated);
        Assert.NotEmpty(secret);
        Assert.NotEqual("not-a-protected-secret", ev.JoinSecretEnc);
        Assert.True(protector.TryUnprotect(ev.JoinSecretEnc, out var restored));
        Assert.Equal(secret, restored);
    }

    private static Event NewEvent(string joinSecretEnc) => new()
    {
        Code = "ABC123",
        Name = "Test",
        JoinSecretEnc = joinSecretEnc,
        HostId = "host",
        UploadOpensAt = DateTimeOffset.UtcNow.AddHours(-1),
        UploadClosesAt = DateTimeOffset.UtcNow.AddHours(1),
        RevealAt = DateTimeOffset.UtcNow.AddHours(2),
    };
}
