using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>Facewear, tracked per style (GlassesStyle sheet) the way the in-game Facewear window lists it; colour variants are not separate entries.</summary>
public sealed unsafe class FacewearModule : ICollectionModule
{
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public FacewearModule(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public string Id => "facewear";

    public string DisplayName => Strings.Get("Module_Facewear", "Facewear");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var playerState = PlayerState.Instance();
        return playerState != null && playerState->IsGlassesStyleUnlocked((ushort)entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var list = dataManager.GetExcelSheet<GlassesStyle>()
            .Where(g => g.RowId > 0 && !g.Name.IsEmpty && g.Icon != 0)
            .OrderBy(g => g.Order).ThenBy(g => g.RowId)
            .Select(g => new CollectionEntry(g.RowId, g.Name.ExtractText(), (uint)g.Icon, g.Order))
            .ToArray();

        log.Debug("Loaded {Count} facewear styles", list.Length);
        return list;
    }
}
