using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>Chocobo bardings (BuddyEquip sheet), unlock state from the companion info.</summary>
public sealed unsafe class BardingModule : ICollectionModule
{
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public BardingModule(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public string Id => "bardings";

    public string DisplayName => Strings.Get("Module_Bardings", "Bardings");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var uiState = UIState.Instance();
        return uiState != null && uiState->Buddy.CompanionInfo.IsBuddyEquipUnlocked(entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var list = dataManager.GetExcelSheet<BuddyEquip>()
            .Where(b => !b.Name.IsEmpty && b.Order > 0)
            .OrderBy(b => b.Order).ThenBy(b => b.RowId)
            .Select(b => new CollectionEntry(b.RowId, b.Name.ExtractText(), FirstIcon(b), b.Order))
            .ToArray();

        log.Debug("Loaded {Count} bardings", list.Length);
        return list;
    }

    private static uint FirstIcon(BuddyEquip b) => b.IconHead != 0 ? b.IconHead : b.IconBody != 0 ? b.IconBody : b.IconLegs;
}
