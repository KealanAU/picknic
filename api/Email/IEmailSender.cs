namespace Picknic.Api.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}

/// <summary>
/// Fallback used when no email provider is configured: logs instead of sending,
/// so invites work in dev without secrets. Swap for Azure Communication Services
/// Email in production.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody)
    {
        logger.LogInformation("Email (not sent — no provider) to {To}: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
