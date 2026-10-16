using NekoDex.Core.Checklist;
using NekoDex.Core.Resets;

namespace NekoDex.Core.Tests;

public class AutoItemTrackerTests
{
    private static DateTimeOffset Utc(int y, int m, int d, int h, int min = 0) => new(y, m, d, h, min, 0, TimeSpan.Zero);

    [Fact]
    public void Reports_last_reading_until_the_next_reset()
    {
        var tracker = new AutoItemTracker();
        tracker.Observe("roulette:5", new AutoItemReading(true), Utc(2026, 10, 16, 12));

        Assert.True(tracker.Current("roulette:5", ResetCadence.Daily, Utc(2026, 10, 16, 14, 59))!.Complete);
        Assert.Null(tracker.Current("roulette:5", ResetCadence.Daily, Utc(2026, 10, 16, 15)));
        Assert.Null(tracker.Current("roulette:5", ResetCadence.Daily, Utc(2026, 10, 16, 16)));
    }

    [Fact]
    public void Reading_taken_after_reset_survives_until_the_following_one()
    {
        var tracker = new AutoItemTracker();
        tracker.Observe("tribal", new AutoItemReading(false, "3 left"), Utc(2026, 10, 16, 15, 1));

        var reading = tracker.Current("tribal", ResetCadence.Daily, Utc(2026, 10, 17, 14, 59));
        Assert.NotNull(reading);
        Assert.False(reading.Complete);
        Assert.Equal("3 left", reading.Detail);
        Assert.Null(tracker.Current("tribal", ResetCadence.Daily, Utc(2026, 10, 17, 15)));
    }

    [Fact]
    public void Weekly_items_follow_the_tuesday_reset()
    {
        var tracker = new AutoItemTracker();
        tracker.Observe("bingo", new AutoItemReading(true, "9/9"), Utc(2026, 10, 19, 10));

        Assert.NotNull(tracker.Current("bingo", ResetCadence.Weekly, Utc(2026, 10, 20, 7, 59)));
        Assert.Null(tracker.Current("bingo", ResetCadence.Weekly, Utc(2026, 10, 20, 8)));
    }

    [Fact]
    public void Never_cadence_never_goes_stale_and_clear_forgets()
    {
        var tracker = new AutoItemTracker();
        tracker.Observe("x", new AutoItemReading(true), Utc(2020, 1, 1, 0));

        Assert.NotNull(tracker.Current("x", ResetCadence.Never, Utc(2026, 10, 16, 12)));
        tracker.Clear();
        Assert.Null(tracker.Current("x", ResetCadence.Never, Utc(2026, 10, 16, 12)));
    }

    [Fact]
    public void Unknown_item_is_null()
    {
        Assert.Null(new AutoItemTracker().Current("nope", ResetCadence.Daily, Utc(2026, 10, 16, 12)));
    }
}
