namespace NekoDex.Core.Unlock;

/// <summary>
/// Maps the free-text categories used by the dataset (FFXIV Collect's source types) to the coarse <see cref="UnlockSourceType"/>.
/// Unknown categories map to <see cref="UnlockSourceType.Other"/>; the generator warns about them so this table stays current.
/// </summary>
public static class UnlockCategories
{
    private static readonly Dictionary<string, UnlockSourceType> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quest"] = UnlockSourceType.Quest,

        ["Dungeon"] = UnlockSourceType.Duty,
        ["Trial"] = UnlockSourceType.Duty,
        ["Raid"] = UnlockSourceType.Duty,
        ["Chaotic Raid"] = UnlockSourceType.Duty,
        ["Deep Dungeon"] = UnlockSourceType.Duty,
        ["V&C Dungeon"] = UnlockSourceType.Duty,
        ["Treasure Hunt"] = UnlockSourceType.Duty,
        ["Bozja"] = UnlockSourceType.Duty,
        ["Eureka"] = UnlockSourceType.Duty,
        ["Occult Crescent"] = UnlockSourceType.Duty,

        ["Purchase"] = UnlockSourceType.Vendor,
        ["Trade"] = UnlockSourceType.Vendor,
        ["Gold Saucer"] = UnlockSourceType.Vendor,
        ["Tribal"] = UnlockSourceType.Vendor,
        ["Hunts"] = UnlockSourceType.Vendor,
        ["Wondrous Tails"] = UnlockSourceType.Vendor,
        ["Island Sanctuary"] = UnlockSourceType.Vendor,
        ["Skybuilders"] = UnlockSourceType.Vendor,
        ["Cosmic Exploration"] = UnlockSourceType.Vendor,
        ["Voyages"] = UnlockSourceType.Vendor,

        ["Achievement"] = UnlockSourceType.Achievement,
        ["Event"] = UnlockSourceType.Event,
        ["Premium"] = UnlockSourceType.Store,

        ["Venture"] = UnlockSourceType.Other,
        ["NPC"] = UnlockSourceType.Other,
        ["Duty"] = UnlockSourceType.Duty,
        ["Beastmaster"] = UnlockSourceType.Other,
        ["PvP"] = UnlockSourceType.Other,
        ["FATE"] = UnlockSourceType.Other,
        ["Gathering"] = UnlockSourceType.Other,
        ["Crafting"] = UnlockSourceType.Other,
        ["Other"] = UnlockSourceType.Other,
        ["Legacy"] = UnlockSourceType.Legacy,
    };

    public static bool IsKnown(string category) => Map.ContainsKey(category);

    public static UnlockSourceType ToType(string category)
        => Map.TryGetValue(category, out var type) ? type : UnlockSourceType.Other;
}
