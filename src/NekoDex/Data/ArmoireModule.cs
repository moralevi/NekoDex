using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>
/// Everything the Armoire can hold (Cabinet sheet), with "owned" meaning stored in the Armoire.
/// The game only loads Armoire contents after the player opens it, so until then the module reports itself unavailable.
/// Entry ids are item ids so links and the outfit planner can use them; the cabinet row id is kept internally.
/// </summary>
public sealed unsafe class ArmoireModule : ICollectionModule
{
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;
    private Dictionary<uint, uint>? cabinetIdByItemId;

    public ArmoireModule(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public string Id => "armoire";

    public string DisplayName => Strings.Get("Module_Armoire", "Armoire");

    public string? UnavailableReason => IsLoaded ? null : Strings.Get("Armoire_NotLoaded", "Open the Armoire (at an inn) once this session to load its contents.");

    public static bool IsLoaded
    {
        get
        {
            var uiState = UIState.Instance();
            return uiState != null && uiState->Cabinet.IsCabinetLoaded();
        }
    }

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry) => IsItemInArmoire(entry.Id);

    /// <summary>Whether the item with this id is stored in the Armoire. False when the Armoire is not loaded or the item cannot go there.</summary>
    public bool IsItemInArmoire(uint itemId)
    {
        GetEntries();
        if (!cabinetIdByItemId!.TryGetValue(itemId, out var cabinetId))
        {
            return false;
        }

        var uiState = UIState.Instance();
        return uiState != null && uiState->Cabinet.IsCabinetLoaded() && uiState->Cabinet.IsItemInCabinet(cabinetId);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var map = new Dictionary<uint, uint>();
        var list = new List<CollectionEntry>();

        foreach (var row in dataManager.GetExcelSheet<Lumina.Excel.Sheets.Cabinet>().Where(c => c.Item.RowId > 0 && c.Item.IsValid).OrderBy(c => c.Category.RowId).ThenBy(c => c.Order))
        {
            var item = row.Item.Value;
            if (item.Name.IsEmpty)
            {
                continue;
            }

            map[item.RowId] = row.RowId;
            list.Add(new CollectionEntry(item.RowId, item.Name.ExtractText(), item.Icon, (int)row.Order));
        }

        cabinetIdByItemId = map;
        log.Debug("Loaded {Count} armoire items from the Cabinet sheet", list.Count);
        return list;
    }
}
