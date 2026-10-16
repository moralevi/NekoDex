namespace NekoDex.Core.Resets;

/// <summary>How often a checklist item clears itself.</summary>
public enum ResetCadence
{
    Never = 0,
    Daily = 1,
    Weekly = 2,
}

/// <summary>
/// A recurring server reset, fixed in UTC. <paramref name="Day"/> null means every day.
/// </summary>
public sealed record ResetDefinition(string Id, string Name, TimeSpan TimeOfDayUtc, DayOfWeek? Day = null)
{
    /// <summary>Next reset strictly after <paramref name="nowUtc"/>.</summary>
    public DateTimeOffset NextAfter(DateTimeOffset nowUtc) => ResetClock.NextOccurrence(nowUtc, TimeOfDayUtc, Day);

    /// <summary>Most recent reset at or before <paramref name="nowUtc"/>.</summary>
    public DateTimeOffset LastAtOrBefore(DateTimeOffset nowUtc) => ResetClock.LastOccurrence(nowUtc, TimeOfDayUtc, Day);
}

/// <summary>The resets NekoDex tracks. Times are the game's fixed UTC schedule; decided for Phase 5 on 2026-10-16.</summary>
public static class KnownResets
{
    public static readonly ResetDefinition Daily = new("daily", "Daily reset", new TimeSpan(15, 0, 0));
    public static readonly ResetDefinition Weekly = new("weekly", "Weekly reset", new TimeSpan(8, 0, 0), DayOfWeek.Tuesday);
    public static readonly ResetDefinition GrandCompany = new("gc", "Grand Company missions", new TimeSpan(20, 0, 0));
    public static readonly ResetDefinition JumboCactpot = new("cactpot", "Jumbo Cactpot drawing", new TimeSpan(19, 0, 0), DayOfWeek.Saturday);
    public static readonly ResetDefinition FashionReport = new("fashion", "Fashion Report judging opens", new TimeSpan(8, 0, 0), DayOfWeek.Friday);

    public static readonly IReadOnlyList<ResetDefinition> All = [Daily, Weekly, GrandCompany, JumboCactpot, FashionReport];

    public static ResetDefinition? For(ResetCadence cadence) => cadence switch
    {
        ResetCadence.Daily => Daily,
        ResetCadence.Weekly => Weekly,
        _ => null,
    };
}
