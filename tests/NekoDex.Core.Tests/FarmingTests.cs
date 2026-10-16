using NekoDex.Core.Collections;
using NekoDex.Core.Farming;
using NekoDex.Core.Unlock;

namespace NekoDex.Core.Tests;

public class FarmingTests
{
    [Theory]
    [InlineData("the Unmaking (Extreme)", 4u, DutyKind.Extreme)]
    [InlineData("Hell on Rails (Extreme)", 4u, DutyKind.Extreme)]
    [InlineData("the Minstrel's Ballad: Necron's Embrace", 4u, DutyKind.Extreme)]
    [InlineData("AAC Heavyweight M4 (Savage)", 5u, DutyKind.Savage)]
    [InlineData("AAC Heavyweight M4", 5u, DutyKind.Raid)]
    [InlineData("the Windward Wilds", 4u, DutyKind.Trial)]
    [InlineData("Sastasha", 2u, DutyKind.Dungeon)]
    [InlineData("Another Merchant's Tale", 30u, DutyKind.Dungeon)]
    [InlineData("the Omega Protocol (Ultimate)", 28u, DutyKind.Ultimate)]
    [InlineData("the Cloud of Darkness (Chaotic)", 37u, DutyKind.Chaotic)]
    [InlineData("Leviathan (Unreal)", 4u, DutyKind.Unreal)]
    [InlineData("Vault Oneiron", 9u, DutyKind.Other)]
    public void Classifies_by_name_suffix_and_content_type(string name, uint type, DutyKind expected)
    {
        Assert.Equal(expected, DutyKindClassifier.Classify(name, type));
    }

    private static readonly Dictionary<uint, DutyInfo> Duties = new()
    {
        [1] = new(1, "Sastasha", DutyKind.Dungeon, 15, 0),
        [2] = new(2, "The Unmaking (Extreme)", DutyKind.Extreme, 100, 770),
        [3] = new(3, "AAC M4 (Savage)", DutyKind.Savage, 100, 770),
    };

    private static EntryView View(uint id, bool owned, params uint[] duties)
        => new(new TrackedEntry(new CollectionEntry(id, $"m{id}", 0), owned), new UnlockSource(UnlockSourceType.Duty, "s", DutyIds: duties.Length == 0 ? null : duties), false);

    [Fact]
    public void Groups_missing_entries_by_duty_in_difficulty_order_and_can_hide_high_end()
    {
        var views = new[] { View(10, false, 2), View(11, false, 1, 2), View(12, true, 1), View(13, false, 3), View(14, false) };

        var groups = FarmPlanner.Group(views, id => Duties.GetValueOrDefault(id), hideHighEnd: false);
        Assert.Equal([1u, 2u, 3u], groups.Select(g => g.Duty.Id));
        Assert.Equal([11u], groups[0].Entries.Select(e => e.Entry.Id));
        Assert.Equal([10u, 11u], groups[1].Entries.Select(e => e.Entry.Id));

        var noHighEnd = FarmPlanner.Group(views, id => Duties.GetValueOrDefault(id), hideHighEnd: true);
        Assert.Equal([1u, 2u], noHighEnd.Select(g => g.Duty.Id));
    }

    [Fact]
    public void Kill_counter_counts_per_character_and_duty_and_resets()
    {
        var counter = new KillCounter();

        Assert.Equal(1, counter.Record(1001, 2));
        Assert.Equal(2, counter.Record(1001, 2));
        Assert.Equal(1, counter.Record(1002, 2));
        Assert.Equal(0, counter.Record(0, 2));
        Assert.Equal(2, counter.Count(1001, 2));
        Assert.Equal(0, counter.Count(1001, 3));

        Assert.True(counter.Reset(1001, 2));
        Assert.False(counter.Reset(1001, 2));
        Assert.Equal(0, counter.Count(1001, 2));
        Assert.Equal(1, counter.Count(1002, 2));
    }
}
