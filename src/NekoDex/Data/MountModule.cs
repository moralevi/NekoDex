using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>
/// Mounts from the Mount sheet, ownership from the character's unlock bitfield.
/// Rows with no icon, no name or a negative Order are the ones the in-game Mount Guide hides.
/// </summary>
public sealed unsafe class MountModule : ICollectionModule
{
    private readonly IDataManager dataManager;
    private readonly IClientState clientState;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public MountModule(IDataManager dataManager, IClientState clientState, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.clientState = clientState;
        this.log = log;
    }

    public string Id => "mounts";

    public string DisplayName => Strings.Get("Module_Mounts", "Mounts");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var playerState = PlayerState.Instance();
        return playerState != null && playerState->IsMountUnlocked(entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var titleCase = clientState.ClientLanguage == ClientLanguage.English;
        var sheet = dataManager.GetExcelSheet<Mount>();

        var list = sheet
            .Where(m => m.Icon != 0 && m.Order >= 0 && !m.Singular.IsEmpty)
            .OrderBy(m => m.Order)
            .Select(m =>
            {
                var name = m.Singular.ExtractText();
                return new CollectionEntry(m.RowId, titleCase ? Core.Text.DisplayName.TitleCase(name) : name, m.Icon, m.Order);
            })
            .ToArray();

        log.Debug("Loaded {Count} mounts from the Mount sheet", list.Length);
        return list;
    }
}
