using NekoDex.Core.Unlock;

namespace NekoDex.Data;

/// <summary>
/// A handful of hand-verified unlock sources. This is the Phase 3 stub that proves the UI renders
/// in-game text without a link; the bundled dataset (Phase 4) replaces it.
/// </summary>
internal static class KnownUnlocks
{
    public static StaticUnlockSourceProvider Create() => new StaticUnlockSourceProvider()
        .Add("mounts", 1, new UnlockSource(UnlockSourceType.Quest, "My Little Chocobo (Grand Company quest, rank Private Second Class)"))
        .Add("mounts", 6, new UnlockSource(UnlockSourceType.Quest, "The Ultimate Weapon (main scenario, level 50)"))
        .Add("mounts", 18, new UnlockSource(UnlockSourceType.Vendor, "Jonathas, Old Gridania, for 6 Achievement Certificates"));
}
