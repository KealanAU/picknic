using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Picknic.Api.Models;

namespace Picknic.Api.Auth;

public class JoinSecretProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("picknic.join-secret.v1");

    public string Protect(string secret) => _protector.Protect(secret);
    public string Unprotect(string encrypted) => _protector.Unprotect(encrypted);

    public string UnprotectOrRotate(Event ev, out bool rotated)
    {
        if (TryUnprotect(ev.JoinSecretEnc, out var secret))
        {
            rotated = false;
            return secret;
        }

        rotated = true;
        secret = GuestTokenService.NewSecret();
        ev.JoinSecretEnc = Protect(secret);
        return secret;
    }

    public bool TryUnprotect(string encrypted, out string secret)
    {
        try
        {
            secret = _protector.Unprotect(encrypted);
            return true;
        }
        catch (CryptographicException)
        {
            secret = string.Empty;
            return false;
        }
    }
}
