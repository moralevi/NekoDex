using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using NekoDex.Core.Dresser;
using NekoDex.Core.Outfits;

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
    private DresserSnapshot? live;
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

    /// <summary>Live contents when read this session, otherwise the stored snapshot for this character, otherwise null.</summary>
    private DresserSnapshot? Current
    {
        get
        {
            var contentId = ContentId;
            if (contentId == 0)
            {
                return null;
            }

            return live is not null && liveContentId == contentId ? live : config.Current.Dresser.Get(contentId);
        }
    }

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

        var ids = manager->PrismBoxItemIds;
        var stain0 = manager->PrismBoxStain0Ids;
        var stain1 = manager->PrismBoxStain1Ids;
        var items = new HashSet<uint>();
        var dyes = new Dictionary<uint, List<DyePair>>();
        for (var i = 0; i < ids.Length; i++)
        {
            if (ids[i] == 0)
            {
                continue;
            }

            var itemId = NormalizeItemId(ids[i]);
            items.Add(itemId);
            var dye = new DyePair(i < stain0.Length ? stain0[i] : 0u, i < stain1.Length ? stain1[i] : 0u);
            if (dye.IsUndyed)
            {
                continue;
            }

            if (!dyes.TryGetValue(itemId, out var list))
            {
                dyes[itemId] = list = new List<DyePair>();
            }

            if (!list.Exists(d => d.SameAs(dye)))
            {
                list.Add(dye);
            }
        }

        if (live is not null && liveContentId == contentId && items.SetEquals(live.Items) && SameDyes(live.Dyes, dyes))
        {
            return;
        }

        config.Current.Dresser.Set(contentId, items, DateTimeOffset.UtcNow, playerState.CharacterName, dyes);
        live = config.Current.Dresser.Get(contentId);
        liveContentId = contentId;
        config.Save();
        log.Debug("Glamour Dresser snapshot updated for {Name}: {Count} items", playerState.CharacterName, items.Count);
    }

    public bool Contains(uint itemId) => Current?.Items.Contains(itemId) ?? false;

    /// <summary>Whether the dresser holds the item in this dye (an undyed request matches any copy).</summary>
    public bool HasDye(uint itemId, DyePair dye) => Current?.HasDye(itemId, dye) ?? false;

    private static bool SameDyes(Dictionary<uint, List<DyePair>> a, Dictionary<uint, List<DyePair>> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (item, list) in a)
        {
            if (!b.TryGetValue(item, out var other) || other.Count != list.Count || !list.TrueForAll(d => other.Exists(o => o.SameAs(d))))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>HQ items are stored with an offset of 1,000,000; the planner works with base item ids.</summary>
    private static uint NormalizeItemId(uint id) => id >= 1_000_000 ? id - 1_000_000 : id;
}
