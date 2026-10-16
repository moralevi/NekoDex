using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>
/// Framer's kits (portrait sets). Each kit is an item with ItemAction type 29459 whose AdditionalData is the kit id that
/// <c>PlayerState.IsFramersKitUnlocked</c> takes. Entry ids are the item ids so web links work; the kit id is kept in a map.
/// No unlock data source has an endpoint for kits, so sources come from links only.
/// </summary>
public sealed unsafe class FramersKitModule : ICollectionModule
{
    private const uint KitActionType = 29459;
    private const string NameSuffix = " Framer's Kit";

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;
    private Dictionary<uint, uint>? kitIdByItem;

    public FramersKitModule(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public string Id => "framerkits";

    public string DisplayName => Strings.Get("Module_FramersKits", "Framer's Kits");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        GetEntries();
        var playerState = PlayerState.Instance();
        return playerState != null && kitIdByItem!.TryGetValue(entry.Id, out var kitId) && playerState->IsFramersKitUnlocked(kitId);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var map = new Dictionary<uint, uint>();
        var list = new List<CollectionEntry>();
        foreach (var item in dataManager.GetExcelSheet<Item>())
        {
            if (item.Name.IsEmpty || !item.ItemAction.IsValid || item.ItemAction.Value.Action.RowId != KitActionType)
            {
                continue;
            }

            var name = item.Name.ExtractText();
            if (name.EndsWith(NameSuffix, System.StringComparison.Ordinal))
            {
                name = name[..^NameSuffix.Length];
            }

            map[item.RowId] = item.AdditionalData.RowId;
            list.Add(new CollectionEntry(item.RowId, name, item.Icon, (int)item.RowId));
        }

        kitIdByItem = map;
        log.Debug("Loaded {Count} framer's kits", list.Count);
        return list;
    }
}
