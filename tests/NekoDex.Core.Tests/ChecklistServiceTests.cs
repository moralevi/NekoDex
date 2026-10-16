using NekoDex.Core.Checklist;
using NekoDex.Core.Resets;

namespace NekoDex.Core.Tests;

public class ChecklistServiceTests
{
    private static DateTimeOffset Utc(int y, int m, int d, int h, int min = 0)
        => new(y, m, d, h, min, 0, TimeSpan.Zero);

    private static ChecklistItem Item(ResetCadence cadence, DateTimeOffset? checkedAt)
        => new() { Text = "x", Cadence = cadence, Checked = checkedAt is not null, CheckedAtUtc = checkedAt };

    [Fact]
    public void Ticking_stamps_time_and_unticking_clears_it()
    {
        var item = new ChecklistItem();

        Assert.True(ChecklistService.SetChecked(item, true, Utc(2026, 10, 16, 12)));
        Assert.Equal(Utc(2026, 10, 16, 12), item.CheckedAtUtc);
        Assert.False(ChecklistService.SetChecked(item, true, Utc(2026, 10, 16, 13)));
        Assert.True(ChecklistService.SetChecked(item, false, Utc(2026, 10, 16, 13)));
        Assert.Null(item.CheckedAtUtc);
    }

    [Fact]
    public void Daily_item_clears_at_reset_not_before()
    {
        var item = Item(ResetCadence.Daily, Utc(2026, 10, 16, 14, 59));

        Assert.False(ChecklistService.ShouldClear(item, Utc(2026, 10, 16, 14, 59)));
        Assert.True(ChecklistService.ShouldClear(item, Utc(2026, 10, 16, 15)));
    }

    [Fact]
    public void Daily_item_ticked_after_reset_survives_until_next_reset()
    {
        var item = Item(ResetCadence.Daily, Utc(2026, 10, 16, 15, 1));

        Assert.False(ChecklistService.ShouldClear(item, Utc(2026, 10, 17, 14, 59)));
        Assert.True(ChecklistService.ShouldClear(item, Utc(2026, 10, 17, 15)));
    }

    [Fact]
    public void Weekly_item_clears_only_on_tuesday_reset()
    {
        // Ticked Monday 2026-10-19; Tuesday 08:00 UTC clears it.
        var item = Item(ResetCadence.Weekly, Utc(2026, 10, 19, 10));

        Assert.False(ChecklistService.ShouldClear(item, Utc(2026, 10, 20, 7, 59)));
        Assert.True(ChecklistService.ShouldClear(item, Utc(2026, 10, 20, 8)));

        var afterReset = Item(ResetCadence.Weekly, Utc(2026, 10, 20, 8, 30));
        Assert.False(ChecklistService.ShouldClear(afterReset, Utc(2026, 10, 25, 23)));
        Assert.True(ChecklistService.ShouldClear(afterReset, Utc(2026, 10, 27, 8)));
    }

    [Fact]
    public void Never_cadence_never_clears()
    {
        var item = Item(ResetCadence.Never, Utc(2020, 1, 1, 0));

        Assert.False(ChecklistService.ShouldClear(item, Utc(2026, 10, 16, 12)));
    }

    [Fact]
    public void Checked_without_timestamp_is_cleared()
    {
        var item = new ChecklistItem { Cadence = ResetCadence.Daily, Checked = true, CheckedAtUtc = null };

        Assert.True(ChecklistService.ShouldClear(item, Utc(2026, 10, 16, 12)));
    }

    [Fact]
    public void ApplyResets_clears_due_items_and_reports_change()
    {
        var due = Item(ResetCadence.Daily, Utc(2026, 10, 15, 16));
        var notDue = Item(ResetCadence.Daily, Utc(2026, 10, 16, 16));
        var untouched = Item(ResetCadence.Daily, null);
        var now = Utc(2026, 10, 16, 20);

        Assert.True(ChecklistService.ApplyResets([due, notDue, untouched], now));
        Assert.False(due.Checked);
        Assert.Null(due.CheckedAtUtc);
        Assert.True(notDue.Checked);
        Assert.False(ChecklistService.ApplyResets([due, notDue, untouched], now));
    }
}
