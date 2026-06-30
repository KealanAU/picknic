namespace Picknic.Api.Auth;

/// <summary>Builds the guest-facing join deep link the QR / invite emails point at.</summary>
public class EventLinks(IConfiguration config)
{
    private readonly string _base = (config["Web:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');

    // Secret goes in the URL fragment so it never reaches server logs or Referer headers.
    public string JoinUrl(string code, string secret) =>
        $"{_base}/join/{code}#s={Uri.EscapeDataString(secret)}";
}
