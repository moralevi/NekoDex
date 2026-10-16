namespace NekoDex.Core.Unlock;

/// <summary>External sites that have one page per collectable, addressed by the game's row id.</summary>
public enum UnlockLinkSite
{
    FfxivCollect = 0,
    GarlandTools = 1,
}

/// <summary>Builds the web page URL for an entry on a given site. Pure string building; verified formats are listed in DATA_SOURCES.md.</summary>
public static class UnlockLinks
{
    /// <summary>Returns null when the site has no known page scheme for the module.</summary>
    public static string? BuildUrl(UnlockLinkSite site, string moduleId, uint entryId)
    {
        var segment = SiteSegment(site, moduleId);
        if (segment is null)
        {
            return null;
        }

        return site switch
        {
            UnlockLinkSite.FfxivCollect => $"https://ffxivcollect.com/{segment}/{entryId}",
            UnlockLinkSite.GarlandTools => $"https://www.garlandtools.org/db/#{segment}/{entryId}",
            _ => null,
        };
    }

    /// <summary>The preferred site's page, or any other site's page when the preferred one has none for this module.</summary>
    public static string? BuildUrlWithFallback(UnlockLinkSite preferred, string moduleId, uint entryId)
    {
        var url = BuildUrl(preferred, moduleId, entryId);
        if (url is not null)
        {
            return url;
        }

        foreach (var site in Enum.GetValues<UnlockLinkSite>())
        {
            if (site != preferred && BuildUrl(site, moduleId, entryId) is { } other)
            {
                return other;
            }
        }

        return null;
    }

    public static string SiteName(UnlockLinkSite site) => site switch
    {
        UnlockLinkSite.FfxivCollect => "FFXIV Collect",
        UnlockLinkSite.GarlandTools => "Garland Tools",
        _ => site.ToString(),
    };

    private static string? SiteSegment(UnlockLinkSite site, string moduleId) => (site, moduleId) switch
    {
        (UnlockLinkSite.FfxivCollect, "mounts") => "mounts",
        (UnlockLinkSite.FfxivCollect, "minions") => "minions",
        (UnlockLinkSite.FfxivCollect, "orchestrion") => "orchestrions",
        (UnlockLinkSite.FfxivCollect, "emotes") => "emotes",
        (UnlockLinkSite.FfxivCollect, "bardings") => "bardings",
        (UnlockLinkSite.FfxivCollect, "fashion") => "fashions",
        (UnlockLinkSite.FfxivCollect, "facewear") => "facewear",
        (UnlockLinkSite.FfxivCollect, "triad") => "triad/cards",
        (UnlockLinkSite.GarlandTools, "mounts") => "mount",
        (UnlockLinkSite.GarlandTools, "minions") => "minion",
        (UnlockLinkSite.GarlandTools, "emotes") => "emote",
        (UnlockLinkSite.GarlandTools, "triad") => "tripletriad",
        (UnlockLinkSite.GarlandTools, "armoire") => "item",
        (UnlockLinkSite.GarlandTools, "items") => "item",
        _ => null,
    };
}
