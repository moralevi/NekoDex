using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>
/// Unlockable hairstyles. CharaMakeCustomize has one purchasable row per race and gender variant; the unlock link ties them
/// together and is also the id FFXIV Collect uses. Styles with few variant rows are limited to some races and say so.
/// Ownership comes from <c>UIState.IsUnlockLinkUnlocked</c>. There is no in-game hairstyle guide; FFXIV Collect's count is the reference.
/// </summary>
public sealed unsafe class HairstyleModule : ICollectionModule
{
    private const string HintPrefix = "Modern Aesthetics - ";
    private const int AllRacesThreshold = 16;

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public HairstyleModule(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public string Id => "hairstyles";

    public string DisplayName => Strings.Get("Module_Hairstyles", "Hairstyles");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var uiState = UIState.Instance();
        return uiState != null && uiState->IsUnlockLinkUnlocked(entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var limited = Strings.Get("Hairstyle_Limited", "Some races");
        var all = Strings.Get("Hairstyle_AllRaces", "All races");

        var list = dataManager.GetExcelSheet<CharaMakeCustomize>()
            .Where(c => c.IsPurchasable && c.UnlockLink != 0)
            .GroupBy(c => (uint)c.UnlockLink)
            .Select(g =>
            {
                var first = g.First();
                var name = first.HintItem.IsValid ? first.HintItem.Value.Name.ExtractText() : $"Hairstyle {g.Key}";
                if (name.StartsWith(HintPrefix, StringComparison.Ordinal))
                {
                    name = name[HintPrefix.Length..];
                }

                var rows = g.Count();
                return new CollectionEntry(g.Key, name, first.Icon, (int)g.Key, rows >= AllRacesThreshold ? all : limited);
            })
            .OrderBy(e => e.Id)
            .ToArray();

        log.Debug("Loaded {Count} hairstyles", list.Length);
        return list;
    }
}
