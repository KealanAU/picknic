using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Picknic.Api.Auth;

public class GuestTokenOptions
{
    public const string SectionName = "Auth:Guest";

    /// <summary>Symmetric signing key — must be >= 32 bytes. Override in prod.</summary>
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "picknic";
    public string Audience { get; set; } = "picknic-guests";

    public SymmetricSecurityKey SecurityKey() =>
        new(Encoding.UTF8.GetBytes(SigningKey));
}

/// <summary>
/// Issues short-lived capability tokens for party guests. A guest token is
/// scoped to exactly one event and expires when the upload window closes — so
/// the deadline is enforced by the token itself, not just app logic.
/// </summary>
public class GuestTokenService(IOptions<GuestTokenOptions> options)
{
    public const string EventClaim = "event_id";
    public const string GuestClaim = "guest_id";

    private readonly GuestTokenOptions _opts = options.Value;

    public string Issue(Guid eventId, Guid guestId, DateTimeOffset expiresAt)
    {
        var creds = new SigningCredentials(
            _opts.SecurityKey(), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, guestId.ToString()),
                new Claim(GuestClaim, guestId.ToString()),
                new Claim(ClaimTypes.Role, "guest"),
                new Claim(EventClaim, eventId.ToString()),
            ],
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Reads the guest UUID from a validated guest principal.</summary>
    public static Guid? GuestId(System.Security.Claims.ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(GuestClaim)?.Value, out var id) ? id : null;

    /// <summary>SHA-256 hash used to store / compare a join secret.</summary>
    public static string Hash(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    public static bool VerifyHash(string secret, string hash) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(secret)),
            Encoding.UTF8.GetBytes(hash));

    /// <summary>High-entropy secret carried by the QR code.</summary>
    public static string NewSecret() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(24));
}
