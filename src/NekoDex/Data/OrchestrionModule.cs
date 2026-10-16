using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>Orchestrion rolls: the named Orchestrion rows that have a UI order, sorted by category then order like the in-game list.</summary>
public sealed unsafe class OrchestrionModule : ICollectionModule
{
    private const uint OrchestrionIconId = 60461;

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public OrchestrionModule(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public string Id => "orchestrion";

    public string DisplayName => Strings.Get("Module_Orchestrion", "Orchestrion");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var playerState = PlayerState.Instance();
        return playerState != null && playerState->IsOrchestrionRollUnlocked(entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var uiParams = dataManager.GetExcelSheet<OrchestrionUiparam>();

        var list = dataManager.GetExcelSheet<Orchestrion>()
            .Where(o => !o.Name.IsEmpty)
            .Select(o => (Row: o, Ui: uiParams.GetRowOrDefault(o.RowId)))
            .Where(x => x.Ui is { Order: > 0 })
            .OrderBy(x => x.Ui!.Value.OrchestrionCategory.RowId).ThenBy(x => x.Ui!.Value.Order).ThenBy(x => x.Row.RowId)
            .Select((x, index) => new CollectionEntry(x.Row.RowId, x.Row.Name.ExtractText(), OrchestrionIconId, index))
            .ToArray();

        log.Debug("Loaded {Count} orchestrion rolls", list.Length);
        return list;
    }
}
