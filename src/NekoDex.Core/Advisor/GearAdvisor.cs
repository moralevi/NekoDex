using NekoDex.Core.Outfits;

namespace NekoDex.Core.Advisor;

/// <summary>What a vendor wants for a piece: one currency item and an amount.</summary>
public sealed record GearPrice(uint CurrencyItemId, string CurrencyName, int Amount);

/// <summary>An equippable item as the advisor sees it. <paramref name="JobUsable"/> is pre-resolved by the data layer for the current job.</summary>
public sealed record GearCandidate(uint ItemId, string Name, uint IconId, GearSlot Slot, int ItemLevel, int EquipLevel, bool JobUsable, GearPrice? Price = null);

public sealed record GearSuggestion(GearSlot Slot, GearCandidate? Equipped, IReadOnlyList<GearCandidate> Upgrades)
{
    public GearCandidate? Best => Upgrades.Count > 0 ? Upgrades[0] : null;

    public int EquippedItemLevel => Equipped?.ItemLevel ?? 0;
}

/// <summary>
/// "Simple" advisor as decided for Phase 7: per slot, the highest item level pieces the character can equip
/// right now (job and level) that beat what is equipped. No stat weights, no best-in-slot tables.
/// Phase 18 adds prices from tomestone vendors and an affordability check against what the character holds.
/// </summary>
public static class GearAdvisor
{
    /// <summary>Slots the advisor covers; both ring slots share the same candidates.</summary>
    public static IReadOnlyList<GearSlot> Slots { get; } = OutfitPlanner.SlotOrder;

    public static GearSuggestion Suggest(GearSlot slot, GearCandidate? equipped, IEnumerable<GearCandidate> candidates, int characterLevel, int maxUpgrades = 3)
    {
        var floor = equipped?.ItemLevel ?? 0;
        var equippedId = equipped?.ItemId ?? 0;

        var upgrades = candidates
            .Where(c => c.Slot == slot && c.JobUsable && c.EquipLevel <= characterLevel && c.ItemLevel > floor && c.ItemId != equippedId)
            .OrderByDescending(c => c.ItemLevel)
            .ThenByDescending(c => c.EquipLevel)
            .ThenByDescending(c => c.ItemId)
            .Take(maxUpgrades)
            .ToArray();

        return new GearSuggestion(slot, equipped, upgrades);
    }

    /// <summary>True when the piece has a known price and the character holds at least that much. Null when there is no price.</summary>
    public static bool? Affordable(GearCandidate candidate, Func<uint, int> currencyHeld)
        => candidate.Price is { } price ? currencyHeld(price.CurrencyItemId) >= price.Amount : null;

    /// <summary>Among the upgrades of a suggestion, the best one the character can buy right now, if any.</summary>
    public static GearCandidate? BestAffordable(GearSuggestion suggestion, Func<uint, int> currencyHeld)
        => suggestion.Upgrades.FirstOrDefault(c => Affordable(c, currencyHeld) == true);

    /// <summary>Mean item level over the given equipped pieces, ignoring empty slots. 0 when nothing is equipped.</summary>
    public static int AverageItemLevel(IEnumerable<GearCandidate?> equipped)
    {
        var sum = 0;
        var count = 0;
        foreach (var piece in equipped)
        {
            if (piece is not null)
            {
                sum += piece.ItemLevel;
                count++;
            }
        }

        return count == 0 ? 0 : sum / count;
    }
}
