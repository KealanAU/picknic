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

            await client.SendAsync(Azure.WaitUntil.Completed, message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }
}
