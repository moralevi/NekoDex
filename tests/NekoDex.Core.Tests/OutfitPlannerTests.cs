using NekoDex.Core.Outfits;

namespace NekoDex.Core.Tests;

public class OutfitPlannerTests
{
    private static ItemOwnership Owned(uint id) => id switch
    {
        100 => ItemOwnership.Inventory,
        200 => ItemOwnership.Armoire,
        300 => ItemOwnership.GlamourDresser,
        _ => ItemOwnership.Missing,
    };

    [Fact]
    public void Evaluates_filled_slots_in_display_order_and_skips_empty()
    {
        var outfit = new Outfit
        {
            Name = "Test",
            Items =
            {
                [GearSlot.Feet] = 400,
                [GearSlot.Head] = 100,
                [GearSlot.Body] = 200,
                [GearSlot.Legs] = 0,
                [GearSlot.RingLeft] = 300,
            },
        };

        var statuses = OutfitPlanner.Evaluate(outfit, Owned);

        Assert.Equal([GearSlot.Head, GearSlot.Body, GearSlot.Feet, GearSlot.RingLeft], statuses.Select(s => s.Slot));
        Assert.Equal([ItemOwnership.Inventory, ItemOwnership.Armoire, ItemOwnership.Missing, ItemOwnership.GlamourDresser], statuses.Select(s => s.Ownership));
    }

    [Fact]
    public void Missing_lists_only_unowned_items()
    {
        var outfit = new Outfit { Items = { [GearSlot.Head] = 100, [GearSlot.Body] = 999, [GearSlot.Hands] = 998 } };

        var missing = OutfitPlanner.Missing(outfit, Owned);

        Assert.Equal([999u, 998u], missing.Select(s => s.ItemId));
        Assert.All(missing, s => Assert.True(s.IsMissing));
    }

    [Fact]
    public void Empty_outfit_has_nothing_missing()
    {
        Assert.Empty(OutfitPlanner.Missing(new Outfit(), Owned));
    }
}
