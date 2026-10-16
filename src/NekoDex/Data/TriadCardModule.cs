using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>
/// Triple Triad cards, in card-number order from TripleTriadCardResident, with stars as rank and the card type as category.
/// Icons live at 87000 + card id (verified present for every card). Offers a "winnable from unlocked NPCs" filter.
/// </summary>
public sealed unsafe class TriadCardModule : ICollectionModule, IFilteredModule
{
    private const uint IconBase = 87000;

    private readonly IDataManager dataManager;
    private readonly TriadNpcIndex npcs;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public TriadCardModule(IDataManager dataManager, TriadNpcIndex npcs, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.npcs = npcs;
        this.log = log;
        Filters =
        [
            new EntryFilterOption("winnable", Strings.Get("Triad_Winnable", "Winnable from NPCs you have unlocked"), e => npcs.IsWinnableFromUnlockedNpc(e.Id)),
        ];
    }

    public string Id => "triad";

    public string DisplayName => Strings.Get("Module_Triad", "Triple Triad");

    public string? UnavailableReason => null;

    public IReadOnlyList<EntryFilterOption> Filters { get; }

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var uiState = UIState.Instance();
        return uiState != null && uiState->IsTripleTriadCardUnlocked((ushort)entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var resident = dataManager.GetExcelSheet<TripleTriadCardResident>();

        var list = dataManager.GetExcelSheet<TripleTriadCard>()
            .Where(c => c.RowId > 0 && !c.Name.IsEmpty)
            .Select(c => (Card: c, Order: resident.TryGetRow(c.RowId, out var r) ? r.Order : ushort.MaxValue, Meta: npcs.MetaFor(c.RowId)))
            .OrderBy(x => x.Order).ThenBy(x => x.Card.RowId)
            .Select(x => new CollectionEntry(x.Card.RowId, x.Card.Name.ExtractText(), IconBase + x.Card.RowId, x.Order, x.Meta?.Type, x.Meta?.Stars ?? 0))
            .ToArray();

        log.Debug("Loaded {Count} Triple Triad cards", list.Length);
        return list;
    }
}
