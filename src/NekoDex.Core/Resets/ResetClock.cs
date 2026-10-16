namespace NekoDex.Core.Resets;

/// <summary>Pure reset-time arithmetic in UTC. Every function takes "now" so tests can use fake clocks.</summary>
public static class ResetClock
{
    /// <summary>Next occurrence of the schedule strictly after <paramref name="nowUtc"/>.</summary>
    public static DateTimeOffset NextOccurrence(DateTimeOffset nowUtc, TimeSpan timeOfDayUtc, DayOfWeek? day)
    {
        var now = nowUtc.ToUniversalTime();
        var candidate = new DateTimeOffset(now.Date, TimeSpan.Zero) + timeOfDayUtc;

        if (day is { } wanted)
        {
            var ahead = ((int)wanted - (int)candidate.DayOfWeek + 7) % 7;
            candidate = candidate.AddDays(ahead);
            if (candidate <= now)
            {
                candidate = candidate.AddDays(7);
            }
        }
        else if (candidate <= now)
        {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }

    /// <summary>Most recent occurrence at or before <paramref name="nowUtc"/>.</summary>
    public static DateTimeOffset LastOccurrence(DateTimeOffset nowUtc, TimeSpan timeOfDayUtc, DayOfWeek? day)
    {
        var next = NextOccurrence(nowUtc, timeOfDayUtc, day);
        var last = next.AddDays(day is null ? -1 : -7);
        return last;
    }

    /// <summary>"1d 02:03:04" or "02:03:04"; never negative.</summary>
    public static string FormatCountdown(TimeSpan remaining)
    {
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        var time = $"{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
        return remaining.Days > 0 ? $"{remaining.Days}d {time}" : time;
    }

    /// <summary>The instant in the user's zone, for "resets at 16:00" style display. DST is handled by <see cref="TimeZoneInfo"/>.</summary>
    public static DateTime ToLocal(DateTimeOffset instantUtc, TimeZoneInfo zone)
        => TimeZoneInfo.ConvertTime(instantUtc, zone).DateTime;
}
