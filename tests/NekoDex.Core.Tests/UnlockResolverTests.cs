using NekoDex.Core.Collections;
using NekoDex.Core.Unlock;

namespace NekoDex.Core.Tests;

public class UnlockResolverTests
{
    private static readonly CollectionEntry Behemoth = new(18, "Behemoth", 4008);

    [Fact]
    public void No_provider_answer_gives_unknown_with_link()
    {
        var resolver = new UnlockResolver([], () => UnlockLinkSite.FfxivCollect);

        var source = resolver.Resolve("mounts", Behemoth);

        Assert.True(source.IsUnknown);
        Assert.Equal("https://ffxivcollect.com/mounts/18", source.Url);
    }

    [Fact]
    public void First_provider_with_an_answer_wins_and_keeps_its_url()
    {
        var first = new StaticUnlockSourceProvider();
        var second = new StaticUnlockSourceProvider()
            .Add("mounts", 18, new UnlockSource(UnlockSourceType.Vendor, "Jonathas", "https://example.test/own"));
        var third = new StaticUnlockSourceProvider()
            .Add("mounts", 18, new UnlockSource(UnlockSourceType.Quest, "wrong"));
        var resolver = new UnlockResolver([first, second, third], () => UnlockLinkSite.GarlandTools);

        var source = resolver.Resolve("mounts", Behemoth);

        Assert.Equal(UnlockSourceType.Vendor, source.Type);
        Assert.Equal("https://example.test/own", source.Url);
    }

    [Fact]
    public void Provider_answer_without_url_gets_site_link_using_its_item_id()
    {
        var provider = new StaticUnlockSourceProvider()
            .Add("mounts", 18, new UnlockSource(UnlockSourceType.Vendor, "Jonathas", ItemId: 6990));
        var resolver = new UnlockResolver([provider], () => UnlockLinkSite.GarlandTools);

        var source = resolver.Resolve("mounts", Behemoth);

        Assert.Equal("https://www.garlandtools.org/db/#item/6990", source.Url);
    }

    [Fact]
    public void Garland_preference_without_item_id_falls_back_to_collect()
    {
        var provider = new StaticUnlockSourceProvider()
            .Add("mounts", 18, new UnlockSource(UnlockSourceType.Vendor, "Jonathas"));
        var resolver = new UnlockResolver([provider], () => UnlockLinkSite.GarlandTools);

        Assert.Equal("https://ffxivcollect.com/mounts/18", resolver.Resolve("mounts", Behemoth).Url);
    }

    [Fact]
    public void Module_unknown_to_preferred_site_falls_back_to_another_site()
    {
        var resolver = new UnlockResolver([], () => UnlockLinkSite.GarlandTools);

        var source = resolver.Resolve("orchestrion", new CollectionEntry(5, "x", 0));

        Assert.True(source.IsUnknown);
        Assert.Equal("https://ffxivcollect.com/orchestrions/5", source.Url);
    }

    [Fact]
    public void Module_unknown_to_every_site_gives_unknown_without_link()
    {
        var resolver = new UnlockResolver([], () => UnlockLinkSite.GarlandTools);

        var source = resolver.Resolve("glamour", new CollectionEntry(5, "x", 0));

        Assert.True(source.IsUnknown);
        Assert.Null(source.Url);
    }

    [Fact]
    public void Site_choice_is_read_live()
    {
        var site = UnlockLinkSite.FfxivCollect;
        var provider = new StaticUnlockSourceProvider()
            .Add("mounts", 18, new UnlockSource(UnlockSourceType.Vendor, "Jonathas", ItemId: 6990));
        var resolver = new UnlockResolver([provider], () => site);

        Assert.StartsWith("https://ffxivcollect.com", resolver.Resolve("mounts", Behemoth).Url);
        site = UnlockLinkSite.GarlandTools;
        Assert.StartsWith("https://www.garlandtools.org", resolver.Resolve("mounts", Behemoth).Url);
    }
}
