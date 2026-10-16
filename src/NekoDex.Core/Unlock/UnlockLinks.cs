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
    /// <summary>
    /// Returns null when the site has no page for this entry. FFXIV Collect indexes by the game's row id;
    /// Garland Tools indexes by item, so it needs <paramref name="itemId"/> except for modules whose entries are items already.
    /// </summary>
    public static string? BuildUrl(UnlockLinkSite site, string moduleId, uint entryId, uint? itemId = null)
    {
        switch (site)
        {
            case UnlockLinkSite.FfxivCollect:
                return CollectSegment(moduleId) is { } segment ? $"https://ffxivcollect.com/{segment}/{entryId}" : null;

            case UnlockLinkSite.GarlandTools:
                var item = moduleId is "armoire" or "items" ? entryId : itemId;
                return item is { } id && id != 0 ? $"https://www.garlandtools.org/db/#item/{id}" : null;

            default:
                return null;
        }
    }

    /// <summary>The preferred site's page, or any other site's page when the preferred one has none for this entry.</summary>
    public static string? BuildUrlWithFallback(UnlockLinkSite preferred, string moduleId, uint entryId, uint? itemId = null)
    {
        var url = BuildUrl(preferred, moduleId, entryId, itemId);
        if (url is not null)
        {
            return url;
        }

        foreach (var site in Enum.GetValues<UnlockLinkSite>())
        {
            if (site != preferred && BuildUrl(site, moduleId, entryId, itemId) is { } other)
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

    private static string? CollectSegment(string moduleId) => moduleId switch
    {
        "mounts" => "mounts",
        "minions" => "minions",
        "orchestrion" => "orchestrions",
        "emotes" => "emotes",
        "bardings" => "bardings",
        "fashion" => "fashions",
        "facewear" => "facewear",
        "triad" => "triad/cards",
        _ => null,
    };
}
