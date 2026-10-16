namespace NekoDex.Core.Outfits;

public sealed record OutfitSlotStatus(GearSlot Slot, uint ItemId, ItemOwnership Ownership)
{
    public bool IsMissing => Ownership == ItemOwnership.Missing;
}

/// <summary>Pure outfit evaluation; the ownership lookup is injected so the game is never touched here.</summary>
public static class OutfitPlanner
{
    public static IReadOnlyList<GearSlot> SlotOrder { get; } = Enum.GetValues<GearSlot>();

    /// <summary>One status per filled slot, in display order. Empty slots are skipped.</summary>
    public static IReadOnlyList<OutfitSlotStatus> Evaluate(Outfit outfit, Func<uint, ItemOwnership> ownership)
    {
        var result = new List<OutfitSlotStatus>(outfit.Items.Count);
        foreach (var slot in SlotOrder)
        {
            if (outfit.Items.TryGetValue(slot, out var itemId) && itemId != 0)
            {
                result.Add(new OutfitSlotStatus(slot, itemId, ownership(itemId)));
            }
        }

        return result;
    }

    public static IReadOnlyList<OutfitSlotStatus> Missing(Outfit outfit, Func<uint, ItemOwnership> ownership)
        => Evaluate(outfit, ownership).Where(s => s.IsMissing).ToArray();
}
