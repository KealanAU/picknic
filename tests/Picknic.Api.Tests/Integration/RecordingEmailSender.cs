using System.Collections.Concurrent;
using Picknic.Api.Email;

namespace Picknic.Api.Tests.Integration;

public sealed record SentEmail(string To, string Subject, string HtmlBody);

/// <summary>
/// In-memory stand-in for the outbound email provider so tests can assert on
/// what would have been sent.
/// </summary>
public sealed class RecordingEmailSender : IEmailSender
{
    public ConcurrentQueue<SentEmail> Sent { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        Sent.Enqueue(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}
