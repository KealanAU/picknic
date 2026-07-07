using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Picknic.Api.Tests.Integration;

public class InviteTests(PicknicApiFactory factory) : IClassFixture<PicknicApiFactory>
{
    [Fact]
    public async Task Invalid_addresses_are_skipped_and_valid_ones_sent()
    {
        using var host = await factory.CreateHostClientAsync("invite-skip@test.local");
        var ev = await factory.CreateEventAsync(host);

        var res = await host.PostAsJsonAsync($"/api/events/{ev.Id}/invites", new
        {
            emails = new[]
            {
                "good-one@test.local",
                "not-an-email",
                "Display Name <wrapped@test.local>",
                "good-two@test.local",
            },
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("invited").GetInt32());
        Assert.Equal(2, json.GetProperty("skipped").GetInt32());

        var recipients = factory.Emails.Sent.Select(e => e.To).ToList();
        Assert.Contains("good-one@test.local", recipients);
        Assert.Contains("good-two@test.local", recipients);
        Assert.DoesNotContain(recipients, r => r.Contains("not-an-email") || r.Contains("wrapped"));
    }

    [Fact]
    public async Task More_than_50_addresses_are_rejected()
    {
        using var host = await factory.CreateHostClientAsync("invite-cap@test.local");
        var ev = await factory.CreateEventAsync(host);

        var emails = Enumerable.Range(0, 51).Select(i => $"guest{i}@test.local").ToArray();
        var res = await host.PostAsJsonAsync($"/api/events/{ev.Id}/invites", new { emails });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("at most 50", problem.GetProperty("detail").GetString());
        Assert.DoesNotContain(factory.Emails.Sent, e => e.To.StartsWith("guest"));
    }

    [Fact]
    public async Task Invite_body_html_encodes_the_event_name()
    {
        using var host = await factory.CreateHostClientAsync("invite-encode@test.local");
        var now = DateTimeOffset.UtcNow;
        var create = await host.PostAsJsonAsync("/api/events", new
        {
            name = "<b>Sneaky</b> & Sons",
            uploadOpensAt = now.AddHours(-1),
            uploadClosesAt = now.AddHours(4),
            revealAt = now.AddHours(5),
        });
        create.EnsureSuccessStatusCode();
        var evId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var res = await host.PostAsJsonAsync($"/api/events/{evId}/invites",
            new { emails = new[] { "encode-target@test.local" } });
        res.EnsureSuccessStatusCode();

        var sent = Assert.Single(factory.Emails.Sent, e => e.To == "encode-target@test.local");
        Assert.DoesNotContain("<b>Sneaky</b>", sent.HtmlBody);
        Assert.Contains("&lt;b&gt;Sneaky&lt;/b&gt; &amp; Sons", sent.HtmlBody);
    }
}
