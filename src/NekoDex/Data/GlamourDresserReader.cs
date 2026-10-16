using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace NekoDex.Data;

/// <summary>
/// Glamour Dresser contents. The game fills them only after the dresser has been opened in the current session,
/// so the last seen contents are kept in the configuration and used until a fresh read is possible.
/// </summary>
public sealed unsafe class GlamourDresserReader
{
    private readonly ConfigurationManager config;
    private readonly IPluginLog log;
    private HashSet<uint>? live;
    private int liveCount;

    public GlamourDresserReader(ConfigurationManager config, IPluginLog log)
    {
        this.config = config;
        this.log = log;
    }

    /// <summary>True when the game has the dresser contents in memory right now.</summary>
    public static bool IsLoaded
    {
        get
        {
            var manager = MirageManager.Instance();
            return manager != null && manager->PrismBoxLoaded;
        }
    }

    public DateTimeOffset? SnapshotTakenAt => config.Current.GlamourDresserCapturedAt;

    public int SnapshotCount => config.Current.GlamourDresserItems.Count;

    /// <summary>Refresh from memory when available (cheap: only when the item count changed), otherwise keep the stored snapshot.</summary>
    public void Refresh()
    {
        var manager = MirageManager.Instance();
        if (manager == null || !manager->PrismBoxLoaded)
        {
            return;
        }

        var ids = manager->PrismBoxItemIds;
        var set = new HashSet<uint>();
        foreach (var id in ids)
        {
            if (id != 0)
            {
                set.Add(NormalizeItemId(id));
            }
        }

        if (live is not null && set.Count == liveCount && set.SetEquals(live))
        {
            return;
        }

        live = set;
        liveCount = set.Count;
        config.Current.GlamourDresserItems = new List<uint>(set);
        config.Current.GlamourDresserCapturedAt = DateTimeOffset.UtcNow;
        config.Save();
        log.Debug("Glamour Dresser snapshot updated: {Count} items", set.Count);
    }

    public bool Contains(uint itemId)
    {
        if (live is not null)
        {
            return live.Contains(itemId);
        }

        return config.Current.GlamourDresserItems.Contains(itemId);
    }

    /// <summary>HQ items are stored with an offset of 1,000,000; the planner works with base item ids.</summary>
    private static uint NormalizeItemId(uint id) => id >= 1_000_000 ? id - 1_000_000 : id;
}
