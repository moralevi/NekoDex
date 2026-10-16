using NekoDex.Core.Resets;

namespace NekoDex.Core.Tests;

public class ResetWatcherTests
{
    private static DateTimeOffset Utc(int y, int m, int d, int h, int min = 0, int s = 0) => new(y, m, d, h, min, s, TimeSpan.Zero);

    private static readonly ResetDefinition[] Watched = [KnownResets.Daily, KnownResets.Weekly];

    [Fact]
    public void First_tick_only_primes_even_right_after_a_reset()
    {
        var watcher = new ResetWatcher();

        Assert.Empty(watcher.Tick(Watched, Utc(2026, 10, 16, 15, 0, 1)));
    }

    [Fact]
    public void Crossing_the_daily_boundary_fires_once()
    {
        var watcher = new ResetWatcher();
        watcher.Tick(Watched, Utc(2026, 10, 16, 14, 59, 59));

        var crossed = watcher.Tick(Watched, Utc(2026, 10, 16, 15, 0, 0));
        Assert.Equal(["daily"], crossed.Select(r => r.Id));

        Assert.Empty(watcher.Tick(Watched, Utc(2026, 10, 16, 15, 0, 1)));
        Assert.Empty(watcher.Tick(Watched, Utc(2026, 10, 17, 14, 59, 59)));
        Assert.Equal(["daily"], watcher.Tick(Watched, Utc(2026, 10, 17, 15)).Select(r => r.Id));
    }

    [Fact]
    public void Weekly_and_daily_can_cross_in_the_same_tick()
    {
        // Tuesday 2026-10-20: weekly at 08:00, daily at 15:00. A long gap between ticks crosses both.
        var watcher = new ResetWatcher();
        watcher.Tick(Watched, Utc(2026, 10, 20, 7, 59));

        var crossed = watcher.Tick(Watched, Utc(2026, 10, 20, 16));
        Assert.Equal(["daily", "weekly"], crossed.Select(r => r.Id).Order());
    }

    [Fact]
    public void Clear_forgets_state_so_the_next_tick_primes_again()
    {
        var watcher = new ResetWatcher();
        watcher.Tick(Watched, Utc(2026, 10, 16, 14, 59));
        watcher.Clear();

        Assert.Empty(watcher.Tick(Watched, Utc(2026, 10, 16, 15, 1)));
    }
}
