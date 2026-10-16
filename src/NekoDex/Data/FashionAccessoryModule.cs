using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Collections;

namespace NekoDex.Data;

/// <summary>Fashion accessories (Ornament sheet), same row rule as mounts: icon, name, non-negative order.</summary>
public sealed unsafe class FashionAccessoryModule : ICollectionModule
{
    private readonly IDataManager dataManager;
    private readonly IClientState clientState;
    private readonly IPluginLog log;
    private IReadOnlyList<CollectionEntry>? entries;

    public FashionAccessoryModule(IDataManager dataManager, IClientState clientState, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.clientState = clientState;
        this.log = log;
    }

    public string Id => "fashion";

    public string DisplayName => Strings.Get("Module_Fashion", "Fashion Accessories");

    public string? UnavailableReason => null;

    public IReadOnlyList<CollectionEntry> GetEntries() => entries ??= Load();

    public bool IsOwned(CollectionEntry entry)
    {
        var playerState = PlayerState.Instance();
        return playerState != null && playerState->IsOrnamentUnlocked(entry.Id);
    }

    private IReadOnlyList<CollectionEntry> Load()
    {
        var titleCase = clientState.ClientLanguage == ClientLanguage.English;

        var list = dataManager.GetExcelSheet<Ornament>()
            .Where(o => o.Icon != 0 && o.Order >= 0 && !o.Singular.IsEmpty)
            .OrderBy(o => o.Order).ThenBy(o => o.RowId)
            .Select(o =>
            {
                var name = o.Singular.ExtractText();
                return new CollectionEntry(o.RowId, titleCase ? Core.Text.DisplayName.TitleCase(name) : name, o.Icon, o.Order);
            })
            .ToArray();

        log.Debug("Loaded {Count} fashion accessories", list.Length);
        return list;
    }
}
