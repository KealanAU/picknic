using System.Net;
using System.Net.Http.Json;

namespace Picknic.Api.Tests.Integration;

public class AuthRateLimitTests(PicknicApiFactory factory) : IClassFixture<PicknicApiFactory>
{
    [Fact]
    public async Task Login_is_rate_limited_per_ip()
    {
        // A fixed IP so the fixed-window "auth" policy (10/min in Program.cs)
        // sees every attempt as one client.
        using var client = factory.CreateClientWithIp("10.98.98.98");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++)
        {
            var res = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "nobody@test.local", password = "Wrong-Pass1!" });
            statuses.Add(res.StatusCode);
        }

        Assert.Equal(10, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }
}
