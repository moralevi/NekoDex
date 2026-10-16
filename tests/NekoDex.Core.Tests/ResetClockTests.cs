using NekoDex.Core.Resets;

namespace NekoDex.Core.Tests;

public class ResetClockTests
{
    private static DateTimeOffset Utc(int y, int m, int d, int h, int min = 0, int s = 0)
        => new(y, m, d, h, min, s, TimeSpan.Zero);

    [Fact]
    public void Daily_before_reset_is_same_day()
    {
        var next = KnownResets.Daily.NextAfter(Utc(2026, 10, 16, 14, 59, 59));

        Assert.Equal(Utc(2026, 10, 16, 15), next);
    }

    [Fact]
    public void Daily_at_reset_instant_is_next_day()
    {
        var next = KnownResets.Daily.NextAfter(Utc(2026, 10, 16, 15));

        Assert.Equal(Utc(2026, 10, 17, 15), next);
    }

    [Fact]
    public void Daily_last_occurrence_straddles_boundary()
    {
        Assert.Equal(Utc(2026, 10, 15, 15), KnownResets.Daily.LastAtOrBefore(Utc(2026, 10, 16, 14, 59, 59)));
        Assert.Equal(Utc(2026, 10, 16, 15), KnownResets.Daily.LastAtOrBefore(Utc(2026, 10, 16, 15)));
    }

    [Fact]
    public void Weekly_is_tuesday_0800_utc()
    {
        // 2026-10-16 is a Friday.
        var next = KnownResets.Weekly.NextAfter(Utc(2026, 10, 16, 12));

        Assert.Equal(DayOfWeek.Tuesday, next.DayOfWeek);
        Assert.Equal(Utc(2026, 10, 20, 8), next);
    }

    [Fact]
    public void Weekly_on_tuesday_before_and_after_reset()
    {
        Assert.Equal(Utc(2026, 10, 20, 8), KnownResets.Weekly.NextAfter(Utc(2026, 10, 20, 7, 59, 59)));
        Assert.Equal(Utc(2026, 10, 27, 8), KnownResets.Weekly.NextAfter(Utc(2026, 10, 20, 8)));
        Assert.Equal(Utc(2026, 10, 13, 8), KnownResets.Weekly.LastAtOrBefore(Utc(2026, 10, 20, 7, 59, 59)));
        Assert.Equal(Utc(2026, 10, 20, 8), KnownResets.Weekly.LastAtOrBefore(Utc(2026, 10, 20, 8)));
    }

    [Fact]
    public void Non_utc_input_is_normalised()
    {
        // 16:30 at +02:00 is 14:30 UTC, so the next daily reset is 15:00 UTC the same day.
        var now = new DateTimeOffset(2026, 10, 16, 16, 30, 0, TimeSpan.FromHours(2));

        Assert.Equal(Utc(2026, 10, 16, 15), KnownResets.Daily.NextAfter(now));
    }

    [Theory]
    [InlineData(0, 0, 5, "00:00:05")]
    [InlineData(2, 3, 4, "02:03:04")]
    [InlineData(26, 3, 4, "1d 02:03:04")]
    public void Formats_countdown(int h, int m, int s, string expected)
    {
        Assert.Equal(expected, ResetClock.FormatCountdown(new TimeSpan(h, m, s)));
    }

    [Fact]
    public void Negative_countdown_clamps_to_zero()
    {
        Assert.Equal("00:00:00", ResetClock.FormatCountdown(TimeSpan.FromSeconds(-5)));
    }

    [Fact]
    public void Local_display_follows_dst()
    {
        var london = FindZone("Europe/London", "GMT Standard Time");

        // UK clocks go forward at 01:00 UTC on 2026-03-29.
        var beforeDst = ResetClock.ToLocal(Utc(2026, 3, 28, 15), london);
        var afterDst = ResetClock.ToLocal(Utc(2026, 3, 29, 15), london);

        Assert.Equal(new TimeSpan(15, 0, 0), beforeDst.TimeOfDay);
        Assert.Equal(new TimeSpan(16, 0, 0), afterDst.TimeOfDay);
    }

    [Fact]
    public void Countdown_across_dst_change_is_wall_clock_independent()
    {
        // 23:00 UTC on 28 March to 15:00 UTC on 29 March is 16 hours regardless of the local DST jump.
        var now = Utc(2026, 3, 28, 23);
        var next = KnownResets.Daily.NextAfter(now);

        Assert.Equal(TimeSpan.FromHours(16), next - now);
    }

    private static TimeZoneInfo FindZone(params string[] ids)
    {
        foreach (var id in ids)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        throw new TimeZoneNotFoundException(string.Join(", ", ids));
    }
}
