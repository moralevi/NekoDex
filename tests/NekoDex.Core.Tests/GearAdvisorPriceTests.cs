using NekoDex.Core.Advisor;
using NekoDex.Core.Outfits;

namespace NekoDex.Core.Tests;

public class GearAdvisorPriceTests
{
    private static readonly GearPrice Poetics = new(28, "Poetics", 345);

    private static GearCandidate Make(uint id, int ilvl, GearPrice? price = null)
        => new(id, $"item{id}", 0, GearSlot.Head, ilvl, 90, true, price);

    [Fact]
    public void Affordable_compares_held_amount_to_price_and_is_null_without_price()
    {
        var priced = Make(1, 130, Poetics);

        Assert.True(GearAdvisor.Affordable(priced, _ => 345));
        Assert.False(GearAdvisor.Affordable(priced, _ => 344));
        Assert.Null(GearAdvisor.Affordable(Make(2, 130), _ => 999));
    }

    [Fact]
    public void Best_affordable_skips_better_pieces_you_cannot_buy()
    {
        var expensive = Make(1, 140, new GearPrice(49, "Mnemonics", 825));
        var cheap = Make(2, 130, Poetics);
        var unpriced = Make(3, 125);
        var suggestion = GearAdvisor.Suggest(GearSlot.Head, null, [expensive, cheap, unpriced], 100);

        Assert.Equal(1u, suggestion.Best!.ItemId);
        Assert.Equal(2u, GearAdvisor.BestAffordable(suggestion, id => id == 28 ? 400 : 0)!.ItemId);
        Assert.Null(GearAdvisor.BestAffordable(suggestion, _ => 0));
    }
}
