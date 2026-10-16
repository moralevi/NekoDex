using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using NekoDex.Core.Outfits;

namespace NekoDex.Data;

/// <summary>One equippable item, with the slots an outfit may put it in.</summary>
public sealed record GearItem(uint ItemId, string Name, uint IconId, IReadOnlyList<GearSlot> Slots, byte EquipLevel);

/// <summary>Every named equippable item from the Item sheet, loaded once on first use (about 30k rows).</summary>
public sealed class GearCatalog
{
    private readonly IDataManager dataManager;
    private readonly IPluginLog log;
    private IReadOnlyList<GearItem>? items;
    private Dictionary<uint, GearItem>? byId;
    private Dictionary<GearSlot, GearItem[]>? bySlot;

    public GearCatalog(IDataManager dataManager, IPluginLog log)
    {
        this.dataManager = dataManager;
        this.log = log;
    }

    public GearItem? Find(uint itemId)
    {
        EnsureLoaded();
        return byId!.TryGetValue(itemId, out var item) ? item : null;
    }

    /// <summary>Items that fit <paramref name="slot"/> whose name contains <paramref name="search"/>, newest (highest id) first, capped at <paramref name="limit"/>.</summary>
    public IEnumerable<GearItem> Search(GearSlot slot, string? search, int limit)
    {
        EnsureLoaded();
        var needle = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var matched = 0;
        foreach (var item in bySlot![slot])
        {
            if (needle is not null && !item.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return item;
            if (++matched >= limit)
            {
                yield break;
            }
        }
    }

    private void EnsureLoaded()
    {
        if (items is not null)
        {
            return;
        }

        var list = new List<GearItem>();
        foreach (var item in dataManager.GetExcelSheet<Item>())
        {
            if (item.EquipSlotCategory.RowId == 0 || item.Name.IsEmpty || !item.EquipSlotCategory.IsValid)
            {
                continue;
            }

            var slots = SlotsFor(item.EquipSlotCategory.Value);
            if (slots.Count == 0)
            {
                continue;
            }

            list.Add(new GearItem(item.RowId, item.Name.ExtractText(), item.Icon, slots, item.LevelEquip));
        }

        list.Sort((a, b) => b.ItemId.CompareTo(a.ItemId));
        items = list;
        byId = list.ToDictionary(i => i.ItemId);
        bySlot = OutfitPlanner.SlotOrder.ToDictionary(slot => slot, slot => list.Where(i => i.Slots.Contains(slot)).ToArray());
        log.Debug("Gear catalog loaded: {Count} equippable items", list.Count);
    }

    /// <summary>Maps the sheet's per-slot flags (1 = occupies) to planner slots. Soul crystals and belts are not glamour.</summary>
    private static IReadOnlyList<GearSlot> SlotsFor(EquipSlotCategory category)
    {
        var slots = new List<GearSlot>(2);
        if (category.MainHand == 1) slots.Add(GearSlot.MainHand);
        if (category.OffHand == 1) slots.Add(GearSlot.OffHand);
        if (category.Head == 1) slots.Add(GearSlot.Head);
        if (category.Body == 1) slots.Add(GearSlot.Body);
        if (category.Gloves == 1) slots.Add(GearSlot.Hands);
        if (category.Legs == 1) slots.Add(GearSlot.Legs);
        if (category.Feet == 1) slots.Add(GearSlot.Feet);
        if (category.Ears == 1) slots.Add(GearSlot.Ears);
        if (category.Neck == 1) slots.Add(GearSlot.Neck);
        if (category.Wrists == 1) slots.Add(GearSlot.Wrists);
        if (category.FingerL == 1 || category.FingerR == 1)
        {
            slots.Add(GearSlot.RingLeft);
            slots.Add(GearSlot.RingRight);
        }

        return slots;
    }
}
