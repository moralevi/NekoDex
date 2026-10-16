using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using NekoDex.Core.Outfits;

namespace NekoDex.Data;

/// <summary>Item ids currently equipped, by planner slot. Read-only view of the EquippedItems container.</summary>
public static unsafe class EquippedGearReader
{
    // Slot indices of the EquippedItems container. 5 (waist) and 13 (soul crystal) are not planner slots.
    private static readonly (GearSlot Slot, int Index)[] SlotIndices =
    [
        (GearSlot.MainHand, 0), (GearSlot.OffHand, 1), (GearSlot.Head, 2), (GearSlot.Body, 3), (GearSlot.Hands, 4),
        (GearSlot.Legs, 6), (GearSlot.Feet, 7), (GearSlot.Ears, 8), (GearSlot.Neck, 9), (GearSlot.Wrists, 10),
        (GearSlot.RingRight, 11), (GearSlot.RingLeft, 12),
    ];

    /// <summary>Empty when the container is not available (not logged in).</summary>
    public static Dictionary<GearSlot, uint> Read()
    {
        var result = new Dictionary<GearSlot, uint>();
        var manager = InventoryManager.Instance();
        if (manager == null)
        {
            return result;
        }

        var container = manager->GetInventoryContainer(InventoryType.EquippedItems);
        if (container == null || !container->IsLoaded)
        {
            return result;
        }

        foreach (var (slot, index) in SlotIndices)
        {
            if (index >= container->Size)
            {
                continue;
            }

            var item = container->GetInventorySlot(index);
            if (item != null && item->GetBaseItemId() != 0)
            {
                result[slot] = item->GetBaseItemId();
            }
        }

        return result;
    }
}
