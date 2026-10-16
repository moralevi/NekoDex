using NekoDex.Core.Collections;

namespace NekoDex.Core.Tests;

public class EntryQueryTests
{
    private static readonly TrackedEntry[] Sample =
    [
        new(new CollectionEntry(1, "Company Chocobo", 100), Owned: true),
        new(new CollectionEntry(2, "Fat Chocobo", 101), Owned: false),
        new(new CollectionEntry(3, "Magitek Armor", 102), Owned: true),
        new(new CollectionEntry(4, "Nightmare", 103), Owned: false),
    ];

    [Fact]
    public void All_with_no_search_returns_everything_in_order()
    {
        var result = EntryQuery.Apply(Sample, OwnershipFilter.All, null).ToArray();

        Assert.Equal([1u, 2u, 3u, 4u], result.Select(e => e.Entry.Id));
    }

    [Fact]
    public void Owned_filter_keeps_only_owned()
    {
        var result = EntryQuery.Apply(Sample, OwnershipFilter.Owned, "").ToArray();

        Assert.All(result, e => Assert.True(e.Owned));
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void Missing_filter_keeps_only_missing()
    {
        var result = EntryQuery.Apply(Sample, OwnershipFilter.Missing, "   ").ToArray();

        Assert.Equal([2u, 4u], result.Select(e => e.Entry.Id));
    }

    [Fact]
    public void Search_is_case_insensitive_substring_and_trims()
    {
        var result = EntryQuery.Apply(Sample, OwnershipFilter.All, "  CHOCOBO ").ToArray();

        Assert.Equal([1u, 2u], result.Select(e => e.Entry.Id));
    }

    [Fact]
    public void Filter_and_search_combine()
    {
        var result = EntryQuery.Apply(Sample, OwnershipFilter.Missing, "chocobo").ToArray();

        var single = Assert.Single(result);
        Assert.Equal(2u, single.Entry.Id);
    }

    [Fact]
    public void Unknown_filter_throws_eagerly()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EntryQuery.Apply(Sample, (OwnershipFilter)99, null));
    }
}
