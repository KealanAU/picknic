namespace Picknic.Api.Email;

/// <summary>
/// Bound from the "Email" config section. When both the ACS connection string
/// and sender address are set, the real sender is wired; otherwise we fall back
/// to <see cref="LoggingEmailSender"/> so dev works without secrets.
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string ConnectionString { get; set; } = "";

    public string FromAddress { get; set; } = "";

    public bool Enabled => !string.IsNullOrWhiteSpace(ConnectionString) && !string.IsNullOrWhiteSpace(FromAddress);
}
