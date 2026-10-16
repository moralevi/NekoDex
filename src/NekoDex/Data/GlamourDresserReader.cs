using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using NekoDex.Core.Dresser;

namespace NekoDex.Data;

/// <summary>
/// Glamour Dresser contents for the logged-in character. The game fills them only after the dresser has been opened
/// in the current session, so the last seen contents are kept per character in the configuration and used until a fresh read is possible.
/// </summary>
public sealed unsafe class GlamourDresserReader
{
    private readonly ConfigurationManager config;
    private readonly IPlayerState playerState;
    private readonly IPluginLog log;
    private HashSet<uint>? live;
    private ulong liveContentId;

    public GlamourDresserReader(ConfigurationManager config, IPlayerState playerState, IPluginLog log)
    {
        this.config = config;
        this.playerState = playerState;
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

    private ulong ContentId => playerState.IsLoaded ? playerState.ContentId : 0;

    private DresserSnapshot? Current => ContentId == 0 ? null : config.Current.Dresser.Get(ContentId);

    public DateTimeOffset? SnapshotTakenAt => Current?.CapturedAt;

    public int SnapshotCount => Current?.Items.Count ?? 0;

    /// <summary>Drop the in-memory set; called on login and logout so one character's dresser never shows for another.</summary>
    public void Reset()
    {
        live = null;
        liveContentId = 0;
    }

    /// <summary>Adopt any pre-v2 snapshot for this character, then refresh from memory when the game has the dresser loaded.</summary>
    public void Refresh()
    {
        var contentId = ContentId;
        if (contentId == 0)
        {
            return;
        }

        if (config.Current.Dresser.AdoptLegacy(contentId, playerState.CharacterName))
        {
            config.Save();
            log.Information("Legacy dresser snapshot assigned to {Name}", playerState.CharacterName);
        }

        var manager = MirageManager.Instance();
        if (manager == null || !manager->PrismBoxLoaded)
        {
            return;
        }

        var set = new HashSet<uint>();
        foreach (var id in manager->PrismBoxItemIds)
        {
            if (id != 0)
            {
                set.Add(NormalizeItemId(id));
            }
        }

        if (live is not null && liveContentId == contentId && set.SetEquals(live))
        {
            return;
        }

        live = set;
        liveContentId = contentId;
        config.Current.Dresser.Set(contentId, set, DateTimeOffset.UtcNow, playerState.CharacterName);
        config.Save();
        log.Debug("Glamour Dresser snapshot updated for {Name}: {Count} items", playerState.CharacterName, set.Count);
    }

    public bool Contains(uint itemId)
    {
        var contentId = ContentId;
        if (contentId == 0)
        {
            return false;
        }

        if (live is not null && liveContentId == contentId)
        {
            return live.Contains(itemId);
        }

        return Current?.Items.Contains(itemId) ?? false;
    }

    /// <summary>HQ items are stored with an offset of 1,000,000; the planner works with base item ids.</summary>
    private static uint NormalizeItemId(uint id) => id >= 1_000_000 ? id - 1_000_000 : id;
}
