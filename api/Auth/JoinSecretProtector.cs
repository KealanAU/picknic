using Microsoft.AspNetCore.DataProtection;

namespace Picknic.Api.Auth;

public class JoinSecretProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("picknic.join-secret.v1");

    public string Protect(string secret) => _protector.Protect(secret);
    public string Unprotect(string encrypted) => _protector.Unprotect(encrypted);
}
