using Picknic.Api.Models;

namespace Picknic.Api.Tests;

public class PlanLimitsTests
{
    [Fact]
    public void Free_tier_limits()
    {
        var l = PlanLimits.For("free");
        Assert.Equal(25, l.MaxGuests);
        Assert.Equal(30, l.MaxPhotosPerGuest);
    }

    [Fact]
    public void Premium_tier_limits()
    {
        var l = PlanLimits.For("premium");
        Assert.Equal(150, l.MaxGuests);
        Assert.Equal(200, l.MaxPhotosPerGuest);
    }

    [Fact]
    public void Tier_lookup_is_case_insensitive()
    {
        Assert.Equal(PlanLimits.For("pro"), PlanLimits.For("PRO"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("legacy-unknown")]
    public void Unknown_or_null_tier_falls_back_to_free(string? tier)
    {
        Assert.Equal(PlanLimits.For("free"), PlanLimits.For(tier));
    }
}
