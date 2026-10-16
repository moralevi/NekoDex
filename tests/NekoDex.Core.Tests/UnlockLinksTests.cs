using NekoDex.Core.Unlock;

namespace NekoDex.Core.Tests;

public class UnlockLinksTests
{
    [Theory]
    [InlineData(UnlockLinkSite.FfxivCollect, "mounts", 18u, "https://ffxivcollect.com/mounts/18")]
    [InlineData(UnlockLinkSite.FfxivCollect, "minions", 1u, "https://ffxivcollect.com/minions/1")]
    [InlineData(UnlockLinkSite.GarlandTools, "mounts", 18u, "https://www.garlandtools.org/db/#mount/18")]
    [InlineData(UnlockLinkSite.GarlandTools, "armoire", 2897u, "https://www.garlandtools.org/db/#item/2897")]
    public void Builds_known_site_urls(UnlockLinkSite site, string moduleId, uint id, string expected)
    {
        Assert.Equal(expected, UnlockLinks.BuildUrl(site, moduleId, id));
    }

    [Fact]
    public void Unknown_module_yields_no_url()
    {
        Assert.Null(UnlockLinks.BuildUrl(UnlockLinkSite.FfxivCollect, "glamour", 1));
        Assert.Null(UnlockLinks.BuildUrl(UnlockLinkSite.GarlandTools, "orchestrion", 1));
    }

    [Fact]
    public void Fallback_uses_another_site_when_preferred_has_none()
    {
        Assert.Equal("https://www.garlandtools.org/db/#item/2897", UnlockLinks.BuildUrlWithFallback(UnlockLinkSite.FfxivCollect, "armoire", 2897));
        Assert.Equal("https://ffxivcollect.com/mounts/1", UnlockLinks.BuildUrlWithFallback(UnlockLinkSite.FfxivCollect, "mounts", 1));
        Assert.Null(UnlockLinks.BuildUrlWithFallback(UnlockLinkSite.FfxivCollect, "glamour", 1));
    }
}
