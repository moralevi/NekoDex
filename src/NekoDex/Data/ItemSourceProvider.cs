using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;
using NekoDex.Core.Unlock;

namespace NekoDex.Data;

/// <summary>
/// Where-to-get for items, derived from game data alone: crafting recipes (RecipeLookup) and gil vendors (GilShopItem).
/// Anything else returns null, so the resolver shows Unknown plus a Garland Tools link. Answers the "items" and "armoire" modules.
/// </summary>
public sealed class ItemSourceProvider : IUnlockSourceProvider
{
    private static readonly string[] CraftJobs = ["CRP", "BSM", "ARM", "GSM", "LTW", "WVR", "ALC", "CUL"];

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private Dictionary<uint, string[]>? craftableBy;
    private HashSet<uint>? gilShop;

    public ItemSourceProvider(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public UnlockSource? GetSource(string moduleId, CollectionEntry entry)
    {
        if (moduleId is not ("items" or "armoire"))
        {
            return null;
        }

        EnsureLoaded();
        var lines = new List<string>(2);
        UnlockSourceType type = UnlockSourceType.Unknown;

        if (gilShop!.Contains(entry.Id))
        {
            lines.Add(Strings.Get("ItemSource_GilShop", "Sold by a gil vendor"));
            type = UnlockSourceType.Vendor;
        }

        if (craftableBy!.TryGetValue(entry.Id, out var jobs))
        {
            lines.Add(string.Format(Strings.Get("ItemSource_Craft", "Crafted by {0}"), string.Join(", ", jobs)));
            if (type == UnlockSourceType.Unknown)
            {
                type = UnlockSourceType.Other;
            }
        }

        if (lines.Count == 0)
        {
            return null;
        }

        return new UnlockSource(type, lines[0], Details: lines, Note: Strings.Get("ItemSource_Note", "From game data (recipes and gil shops)."));
    }

    private void EnsureLoaded()
    {
        if (craftableBy is not null)
        {
            return;
        }

        var crafts = new Dictionary<uint, string[]>();
        foreach (var row in dataManager.GetExcelSheet<RecipeLookup>())
        {
            var jobs = new List<string>(2);
            var refs = new[] { row.CRP, row.BSM, row.ARM, row.GSM, row.LTW, row.WVR, row.ALC, row.CUL };
            for (var i = 0; i < refs.Length; i++)
            {
                if (refs[i].RowId != 0)
                {
                    jobs.Add(CraftJobs[i]);
                }
            }

            if (jobs.Count > 0)
            {
                crafts[row.RowId] = jobs.ToArray();
            }
        }

        var shop = new HashSet<uint>();
        foreach (var subrows in dataManager.GetSubrowExcelSheet<GilShopItem>())
        {
            foreach (var row in subrows)
            {
                if (row.Item.RowId != 0)
                {
                    shop.Add(row.Item.RowId);
                }
            }
        }

        craftableBy = crafts;
        gilShop = shop;
        log.Debug("Item sources loaded: {Crafts} craftable items, {Shop} gil shop items", crafts.Count, shop.Count);
    }
}
