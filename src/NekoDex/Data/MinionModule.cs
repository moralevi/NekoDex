using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>Minions from the Companion sheet, ownership from the character's unlock bitfield. Same row rule as mounts (icon, name, order).</summary>
public sealed unsafe class MinionModule : ICollectionModule
{
    private readonly IDataManager dataManager;
    private readonly IClientState clientState;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public MinionModule(IDataManager dataManager, IClientState clientState, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.clientState = clientState;
        this.log = log;
    }

    public string Id => "minions";

    public string DisplayName => Strings.Get("Module_Minions", "Minions");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var uiState = UIState.Instance();
        return uiState != null && uiState->IsCompanionUnlocked(entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var titleCase = clientState.ClientLanguage == ClientLanguage.English;

        var list = dataManager.GetExcelSheet<Companion>()
            .Where(c => c.Icon != 0 && c.Order > 0 && !c.Singular.IsEmpty)
            .OrderBy(c => c.Order).ThenBy(c => c.RowId)
            .Select(c =>
            {
                var name = c.Singular.ExtractText();
                return new CollectionEntry(c.RowId, titleCase ? Core.Text.DisplayName.TitleCase(name) : name, c.Icon, c.Order);
            })
            .ToArray();

        log.Debug("Loaded {Count} minions from the Companion sheet", list.Length);
        return list;
    }
}
