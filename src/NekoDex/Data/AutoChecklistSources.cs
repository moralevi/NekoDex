using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using NekoDex.Core.Checklist;
using NekoDex.Core.Resets;

namespace NekoDex.Data;

/// <summary>
/// Completion states the game exposes read-only. Verified 2026-10-16 against FFXIVClientStructs for Dalamud API 15:
/// duty roulettes (InstanceContent), tribal allowances (QuestManager) and Wondrous Tails stickers (PlayerState).
/// Not exposed and therefore not offered: Mini Cactpot plays, Jumbo Cactpot tickets, Grand Company turn-ins.
/// </summary>
public sealed unsafe class AutoChecklistSources
{
    public const string TribalId = "tribal";
    public const string BingoId = "bingo";
    private const string RoulettePrefix = "roulette:";
    private const int TribalAllowancesPerDay = 12;
    private const int BingoStickersForCompletion = 9;

    // Duty roulettes in the order the Duty Finder lists them. Chocobo races are left out.
    private static readonly uint[] RouletteIds = [5, 8, 2, 1, 6, 3, 4, 15, 17, 9, 7];

    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<AutoItemDefinition>? definitions;

    public AutoChecklistSources(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public static IReadOnlyList<string> DefaultEnabled { get; } = [RoulettePrefix + "5", RoulettePrefix + "1", TribalId];

    public IReadOnlyList<AutoItemDefinition> Definitions => definitions ??= Load();

    /// <summary>Null when the game cannot answer right now (not logged in, or an unknown id).</summary>
    public AutoItemReading? Read(string id)
    {
        if (id.StartsWith(RoulettePrefix) && byte.TryParse(id.AsSpan(RoulettePrefix.Length), out var roulette))
        {
            var uiState = UIState.Instance();
            return uiState == null ? null : new AutoItemReading(uiState->InstanceContent.IsRouletteComplete(roulette));
        }

        switch (id)
        {
            case TribalId:
            {
                var quests = QuestManager.Instance();
                if (quests == null)
                {
                    return null;
                }

                var left = (int)quests->GetBeastTribeAllowance();
                return new AutoItemReading(left == 0, string.Format(Strings.Get("Auto_TribalLeft", "{0} of {1} left"), left, TribalAllowancesPerDay));
            }

            case BingoId:
            {
                var player = PlayerState.Instance();
                if (player == null)
                {
                    return null;
                }

                if (!player->HasWeeklyBingoJournal)
                {
                    return new AutoItemReading(false, Strings.Get("Auto_NoJournal", "no journal"));
                }

                var stickers = player->WeeklyBingoNumPlacedStickers;
                return new AutoItemReading(stickers >= BingoStickersForCompletion, string.Format(Strings.Get("Auto_Stickers", "{0}/{1} stickers"), stickers, BingoStickersForCompletion));
            }

            default:
                return null;
        }
    }

    private IReadOnlyList<AutoItemDefinition> Load()
    {
        var sheet = dataManager.GetExcelSheet<Lumina.Excel.Sheets.ContentRoulette>();
        var list = new List<AutoItemDefinition>();
        foreach (var rouletteId in RouletteIds)
        {
            if (sheet.TryGetRow(rouletteId, out var row) && !row.Name.IsEmpty)
            {
                list.Add(new AutoItemDefinition(RoulettePrefix + rouletteId, row.Name.ExtractText(), ResetCadence.Daily));
            }
        }

        list.Add(new AutoItemDefinition(TribalId, Strings.Get("Auto_Tribal", "Tribal quest allowances"), ResetCadence.Daily));
        list.Add(new AutoItemDefinition(BingoId, Strings.Get("Auto_Bingo", "Wondrous Tails"), ResetCadence.Weekly));
        log.Debug("Auto checklist: {Count} items available", list.Count);
        return list;
    }
}
