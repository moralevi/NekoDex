using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>Unlockable emotes: Emote rows with a name, an icon and an unlock link. Emotes everyone has from the start are left out.</summary>
public sealed unsafe class EmoteModule : ICollectionModule
{
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public EmoteModule(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public string Id => "emotes";

    public string DisplayName => Strings.Get("Module_Emotes", "Emotes");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var uiState = UIState.Instance();
        return uiState != null && uiState->IsEmoteUnlocked((ushort)entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var list = dataManager.GetExcelSheet<Emote>()
            .Where(e => !e.Name.IsEmpty && e.Icon != 0 && e.UnlockLink != 0)
            .OrderBy(e => e.Order).ThenBy(e => e.RowId)
            .Select(e => new CollectionEntry(e.RowId, e.Name.ExtractText(), e.Icon, e.Order))
            .ToArray();

        log.Debug("Loaded {Count} unlockable emotes", list.Length);
        return list;
    }
}
