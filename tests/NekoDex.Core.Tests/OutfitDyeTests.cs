using NekoDex.Core.Dresser;
using NekoDex.Core.Outfits;

namespace NekoDex.Core.Tests;

public class OutfitDyeTests
{
    [Fact]
    public void Setting_an_undyed_pair_removes_the_entry()
    {
        var outfit = new Outfit();
        outfit.SetDye(GearSlot.Body, new DyePair(5, 0));
        Assert.Equal(5u, outfit.DyeFor(GearSlot.Body).Primary);

        outfit.SetDye(GearSlot.Body, new DyePair(0, 0));
        Assert.Empty(outfit.Dyes);
        Assert.True(outfit.DyeFor(GearSlot.Body).IsUndyed);
    }

    [Fact]
    public void Fill_replaces_items_and_dyes_and_skips_empty_slots()
    {
        var outfit = new Outfit { Items = { [GearSlot.Feet] = 9 }, Dyes = { [GearSlot.Feet] = new DyePair(1, 1) } };

        outfit.Fill(
            new Dictionary<GearSlot, uint> { [GearSlot.Head] = 1, [GearSlot.Body] = 2, [GearSlot.Legs] = 0 },
            new Dictionary<GearSlot, DyePair> { [GearSlot.Body] = new(3, 4), [GearSlot.Legs] = new(7, 7) });

        Assert.Equal([GearSlot.Head, GearSlot.Body], outfit.Items.Keys.OrderBy(k => k));
        Assert.Single(outfit.Dyes);
        Assert.Equal(3u, outfit.DyeFor(GearSlot.Body).Primary);
        Assert.Equal(4u, outfit.DyeFor(GearSlot.Body).Secondary);
    }

    [Fact]
    public void Dresser_snapshot_matches_dyes()
    {
        var snapshot = new DresserSnapshot
        {
            Items = [100, 200],
            Dyes = { [100] = [new DyePair(5, 0), new DyePair(5, 6)] },
        };

        Assert.True(snapshot.HasDye(100, new DyePair(0, 0)));
        Assert.True(snapshot.HasDye(100, new DyePair(5, 6)));
        Assert.False(snapshot.HasDye(100, new DyePair(6, 5)));
        Assert.True(snapshot.HasDye(200, new DyePair(0, 0)));
        Assert.False(snapshot.HasDye(200, new DyePair(1, 0)));
        Assert.False(snapshot.HasDye(300, new DyePair(0, 0)));
    }
}
