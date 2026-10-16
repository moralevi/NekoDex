using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace NekoDex.Data;

/// <summary>An NPC that plays Triple Triad and can drop a card.</summary>
public sealed record TriadNpc(
    uint NpcId,
    string Name,
    string Place,
    uint TerritoryId,
    uint MapId,
    Vector3? Position,
    IReadOnlyList<(uint QuestId, string QuestName)> UnlockQuests,
    bool NeedsAllQuests)
{
    public bool HasPosition => Position is not null;
}

/// <summary>Card meta the sheets carry: stars and type.</summary>
public sealed record TriadCardMeta(int Stars, string? Type);

/// <summary>
/// Card to NPC mapping built from the TripleTriad sheet (fixed and variable card lists), NPC identity from ENpcBase's data
/// references, names from ENpcResident, positions from Level rows of type 8. Verified 2026-10-16: 146 NPC rows, 134 with a position.
/// </summary>
public sealed unsafe class TriadNpcIndex
{
    private const byte EventNpcLevelType = 8;

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private Dictionary<uint, List<TriadNpc>>? npcsByCard;
    private Dictionary<uint, TriadCardMeta>? meta;

    public TriadNpcIndex(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public IReadOnlyList<TriadNpc> NpcsFor(uint cardId)
    {
        EnsureLoaded();
        return npcsByCard!.TryGetValue(cardId, out var list) ? list : [];
    }

    public TriadCardMeta? MetaFor(uint cardId)
    {
        EnsureLoaded();
        return meta!.TryGetValue(cardId, out var m) ? m : null;
    }

    /// <summary>Whether the character has done the quests that make this NPC playable.</summary>
    public static bool IsUnlocked(TriadNpc npc)
    {
        if (npc.UnlockQuests.Count == 0)
        {
            return true;
        }

        return npc.NeedsAllQuests
            ? npc.UnlockQuests.All(q => QuestManager.IsQuestComplete(q.QuestId))
            : npc.UnlockQuests.Any(q => QuestManager.IsQuestComplete(q.QuestId));
    }

    public bool IsWinnableFromUnlockedNpc(uint cardId) => NpcsFor(cardId).Any(IsUnlocked);

    private void EnsureLoaded()
    {
        if (npcsByCard is not null)
        {
            return;
        }

        var triad = dataManager.GetExcelSheet<TripleTriad>();
        var triadIds = triad.Select(t => t.RowId).ToHashSet();

        var npcForTriad = new Dictionary<uint, uint>();
        foreach (var npc in dataManager.GetExcelSheet<ENpcBase>())
        {
            foreach (var data in npc.ENpcData)
            {
                if (data.RowId != 0 && triadIds.Contains(data.RowId))
                {
                    npcForTriad.TryAdd(data.RowId, npc.RowId);
                }
            }
        }

        var levelByNpc = new Dictionary<uint, Level>();
        foreach (var level in dataManager.GetExcelSheet<Level>())
        {
            if (level.Type == EventNpcLevelType && level.Object.RowId != 0)
            {
                levelByNpc.TryAdd(level.Object.RowId, level);
            }
        }

        var residents = dataManager.GetExcelSheet<ENpcResident>();
        var byCard = new Dictionary<uint, List<TriadNpc>>();
        foreach (var row in triad)
        {
            if (!npcForTriad.TryGetValue(row.RowId, out var npcId))
            {
                continue;
            }

            var name = residents.TryGetRow(npcId, out var resident) ? resident.Singular.ExtractText() : $"NPC {npcId}";
            string place = string.Empty;
            uint territoryId = 0;
            uint mapId = 0;
            Vector3? position = null;
            if (levelByNpc.TryGetValue(npcId, out var level))
            {
                territoryId = level.Territory.RowId;
                mapId = level.Map.RowId;
                position = new Vector3(level.X, level.Y, level.Z);
                if (level.Territory.IsValid && level.Territory.Value.PlaceName.IsValid)
                {
                    place = level.Territory.Value.PlaceName.Value.Name.ExtractText();
                }
            }

            var quests = row.PreviousQuest
                .Where(q => q.RowId != 0)
                .Select(q => (q.RowId, q.IsValid ? q.Value.Name.ExtractText() : $"Quest {q.RowId}"))
                .ToArray();

            var npc = new TriadNpc(npcId, Core.Text.DisplayName.TitleCase(name), place, territoryId, mapId, position, quests, row.PreviousQuestJoin != 2);
            foreach (var card in row.TripleTriadCardFixed.Concat(row.TripleTriadCardVariable))
            {
                if (card.RowId == 0)
                {
                    continue;
                }

                if (!byCard.TryGetValue(card.RowId, out var list))
                {
                    byCard[card.RowId] = list = new List<TriadNpc>();
                }

                if (list.All(n => n.NpcId != npcId))
                {
                    list.Add(npc);
                }
            }
        }

        var metaByCard = new Dictionary<uint, TriadCardMeta>();
        foreach (var resident in dataManager.GetExcelSheet<TripleTriadCardResident>())
        {
            var stars = resident.TripleTriadCardRarity.IsValid ? resident.TripleTriadCardRarity.Value.Stars : 0;
            var type = resident.TripleTriadCardType.IsValid ? resident.TripleTriadCardType.Value.Name.ExtractText() : string.Empty;
            metaByCard[resident.RowId] = new TriadCardMeta(stars, string.IsNullOrEmpty(type) ? null : type);
        }

        npcsByCard = byCard;
        meta = metaByCard;
        log.Debug("Triple Triad index: {Cards} cards from {Npcs} NPCs", byCard.Count, npcForTriad.Count);
    }
}
