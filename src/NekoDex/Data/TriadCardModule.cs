using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>Triple Triad cards, in card-number order from TripleTriadCardResident. Icons live at 82100 + card id.</summary>
public sealed unsafe class TriadCardModule : ICollectionModule
{
    private const uint IconBase = 82100;

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public TriadCardModule(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public string Id => "triad";

    public string DisplayName => Strings.Get("Module_Triad", "Triple Triad");

    public string? UnavailableReason => null;

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
            .Select(c => (Card: c, Order: resident.TryGetRow(c.RowId, out var r) ? r.Order : ushort.MaxValue))
            .OrderBy(x => x.Order).ThenBy(x => x.Card.RowId)
            .Select(x => new CollectionEntry(x.Card.RowId, x.Card.Name.ExtractText(), IconBase + x.Card.RowId, x.Order))
            .ToArray();

        log.Debug("Loaded {Count} Triple Triad cards", list.Length);
        return list;
    }
}
