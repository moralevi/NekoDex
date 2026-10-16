using NekoDex.Core.Advisor;
using NekoDex.Core.Outfits;

namespace NekoDex.Core.Tests;

public class GearAdvisorTests
{
    private static GearCandidate Make(uint id, GearSlot slot, int ilvl, int equipLevel, bool usable = true)
        => new(id, $"item{id}", 0, slot, ilvl, equipLevel, usable);

    private static readonly GearCandidate[] Pool =
    [
        Make(1, GearSlot.Head, 100, 50),
        Make(2, GearSlot.Head, 120, 60),
        Make(3, GearSlot.Head, 130, 70),
        Make(4, GearSlot.Head, 125, 60, usable: false),
        Make(5, GearSlot.Head, 115, 55),
        Make(6, GearSlot.Body, 200, 60),
        Make(7, GearSlot.Head, 120, 58),
    ];

    [Fact]
    public void Ranks_usable_level_appropriate_upgrades_by_item_level()
    {
        var equipped = Make(1, GearSlot.Head, 100, 50);

        var suggestion = GearAdvisor.Suggest(GearSlot.Head, equipped, Pool, characterLevel: 60);

        // 130 needs level 70; 125 is not usable by the job; 120 twice (higher equip level first), then 115.
        Assert.Equal([2u, 7u, 5u], suggestion.Upgrades.Select(u => u.ItemId));
        Assert.Equal(2u, suggestion.Best!.ItemId);
        Assert.Equal(100, suggestion.EquippedItemLevel);
    }

    [Fact]
    public void Nothing_equipped_means_everything_is_an_upgrade()
    {
        var suggestion = GearAdvisor.Suggest(GearSlot.Head, null, Pool, characterLevel: 100, maxUpgrades: 10);

        Assert.Equal([3u, 2u, 7u, 5u, 1u], suggestion.Upgrades.Select(u => u.ItemId));
        Assert.Equal(0, suggestion.EquippedItemLevel);
    }

    [Fact]
    public void Already_best_in_pool_yields_no_upgrades()
    {
        var equipped = Make(3, GearSlot.Head, 130, 70);

        var suggestion = GearAdvisor.Suggest(GearSlot.Head, equipped, Pool, characterLevel: 100);

        Assert.Empty(suggestion.Upgrades);
        Assert.Null(suggestion.Best);
    }

    [Fact]
    public void Other_slots_are_ignored()
    {
        var suggestion = GearAdvisor.Suggest(GearSlot.Body, null, Pool, characterLevel: 100);

        var single = Assert.Single(suggestion.Upgrades);
        Assert.Equal(6u, single.ItemId);
    }

    [Fact]
    public void Average_item_level_skips_empty_slots()
    {
        GearCandidate?[] equipped = [Make(1, GearSlot.Head, 100, 1), null, Make(6, GearSlot.Body, 200, 1)];

        Assert.Equal(150, GearAdvisor.AverageItemLevel(equipped));
        Assert.Equal(0, GearAdvisor.AverageItemLevel([null, null]));
    }
}
