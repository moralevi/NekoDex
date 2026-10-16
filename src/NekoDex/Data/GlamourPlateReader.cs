using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using NekoDex.Core.Outfits;

namespace NekoDex.Data;

/// <summary>One glamour plate as items and dyes per planner slot.</summary>
public sealed record PlateContents(int Number, IReadOnlyDictionary<GearSlot, uint> Items, IReadOnlyDictionary<GearSlot, DyePair> Dyes)
{
    public bool IsEmpty => Items.Count == 0;
}

/// <summary>Glamour plates from memory. The game fills them only after the Glamour Dresser has been opened in the session.</summary>
public static unsafe class GlamourPlateReader
{
    // Plate slot order: main hand, off hand, head, body, hands, legs, feet, ears, neck, wrists, right ring, left ring.
    private static readonly GearSlot[] SlotOrder =
    [
        GearSlot.MainHand, GearSlot.OffHand, GearSlot.Head, GearSlot.Body, GearSlot.Hands, GearSlot.Legs,
        GearSlot.Feet, GearSlot.Ears, GearSlot.Neck, GearSlot.Wrists, GearSlot.RingRight, GearSlot.RingLeft,
    ];

    public static bool IsLoaded
    {
        get
        {
            var manager = MirageManager.Instance();
            return manager != null && manager->GlamourPlatesLoaded;
        }
    }

    /// <summary>Empty when plates are not loaded.</summary>
    public static IReadOnlyList<PlateContents> Read()
    {
        var manager = MirageManager.Instance();
        if (manager == null || !manager->GlamourPlatesLoaded)
        {
            return [];
        }

        var plates = manager->GlamourPlates;
        var result = new List<PlateContents>(plates.Length);
        for (var p = 0; p < plates.Length; p++)
        {
            ref var plate = ref plates[p];
            var items = new Dictionary<GearSlot, uint>();
            var dyes = new Dictionary<GearSlot, DyePair>();
            var ids = plate.ItemIds;
            var stain0 = plate.Stain0Ids;
            var stain1 = plate.Stain1Ids;
            for (var i = 0; i < SlotOrder.Length && i < ids.Length; i++)
            {
                if (ids[i] == 0)
                {
                    continue;
                }

                items[SlotOrder[i]] = ids[i] >= 1_000_000 ? ids[i] - 1_000_000 : ids[i];
                var dye = new DyePair(i < stain0.Length ? stain0[i] : 0u, i < stain1.Length ? stain1[i] : 0u);
                if (!dye.IsUndyed)
                {
                    dyes[SlotOrder[i]] = dye;
                }
            }

            result.Add(new PlateContents(p + 1, items, dyes));
        }

        return result;
    }
}
