using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace Picknic.Api.Email;

/// <summary>
/// Sends email via Azure Communication Services. Failures are logged, not
/// thrown — a bounced invite shouldn't 500 the request that triggered it.
/// </summary>
public class AcsEmailSender(IOptions<EmailOptions> options, ILogger<AcsEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions opts = options.Value;
    private readonly EmailClient client = new(options.Value.ConnectionString);

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new EmailMessage(
                senderAddress: opts.FromAddress,
                content: new EmailContent(subject) { Html = htmlBody },
                recipients: new EmailRecipients(new[] { new EmailAddress(to) }));

            // Started, not Completed: return once ACS accepts the message rather
            // than blocking the triggering request while delivery is polled. A
            // slow WaitUntil.Completed here stalls inline callers like Identity's
            // /register long enough for the ingress/client to time out (499).
            await client.SendAsync(Azure.WaitUntil.Started, message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }
}
