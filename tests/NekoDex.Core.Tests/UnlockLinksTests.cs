using NekoDex.Core.Unlock;

namespace NekoDex.Core.Tests;

public class UnlockLinksTests
{
    [Theory]
    [InlineData(UnlockLinkSite.FfxivCollect, "mounts", 18u, null, "https://ffxivcollect.com/mounts/18")]
    [InlineData(UnlockLinkSite.FfxivCollect, "minions", 1u, null, "https://ffxivcollect.com/minions/1")]
    [InlineData(UnlockLinkSite.GarlandTools, "mounts", 18u, 6990u, "https://www.garlandtools.org/db/#item/6990")]
    [InlineData(UnlockLinkSite.GarlandTools, "armoire", 2897u, null, "https://www.garlandtools.org/db/#item/2897")]
    public void Builds_known_site_urls(UnlockLinkSite site, string moduleId, uint id, uint? itemId, string expected)
    {
        Assert.Equal(expected, UnlockLinks.BuildUrl(site, moduleId, id, itemId));
    }

    [Fact]
    public void Garland_needs_an_item_id_for_collectables()
    {
        Assert.Null(UnlockLinks.BuildUrl(UnlockLinkSite.GarlandTools, "mounts", 18));
        Assert.Null(UnlockLinks.BuildUrl(UnlockLinkSite.GarlandTools, "orchestrion", 1));
        Assert.Equal("https://www.garlandtools.org/db/#item/14222", UnlockLinks.BuildUrl(UnlockLinkSite.GarlandTools, "orchestrion", 2, 14222));
    }

    [Fact]
    public void Unknown_module_yields_no_url()
    {
        Assert.Null(UnlockLinks.BuildUrl(UnlockLinkSite.FfxivCollect, "glamour", 1));
    }

    [Fact]
    public void Fallback_uses_another_site_when_preferred_has_none()
    {
        Assert.Equal("https://www.garlandtools.org/db/#item/2897", UnlockLinks.BuildUrlWithFallback(UnlockLinkSite.FfxivCollect, "armoire", 2897));
        Assert.Equal("https://ffxivcollect.com/mounts/1", UnlockLinks.BuildUrlWithFallback(UnlockLinkSite.FfxivCollect, "mounts", 1));
        Assert.Equal("https://ffxivcollect.com/mounts/18", UnlockLinks.BuildUrlWithFallback(UnlockLinkSite.GarlandTools, "mounts", 18));
        Assert.Equal("https://www.garlandtools.org/db/#item/6990", UnlockLinks.BuildUrlWithFallback(UnlockLinkSite.GarlandTools, "mounts", 18, 6990));
        Assert.Null(UnlockLinks.BuildUrlWithFallback(UnlockLinkSite.FfxivCollect, "glamour", 1));
    }
}
