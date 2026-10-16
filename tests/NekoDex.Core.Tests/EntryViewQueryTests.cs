using NekoDex.Core.Collections;
using NekoDex.Core.Unlock;

namespace NekoDex.Core.Tests;

public class EntryViewQueryTests
{
    private static EntryView View(uint id, string name, bool owned, UnlockSourceType type, string? patch, bool pinned = false, params string[] categories)
        => new(
            new TrackedEntry(new CollectionEntry(id, name, 0), owned),
            new UnlockSource(type, "s", Patch: patch, Categories: categories.Length == 0 ? null : categories, Details: categories.Select(c => $"{c}: x").ToArray()),
            pinned);

    private static readonly EntryView[] Sample =
    [
        View(1, "Company Chocobo", true, UnlockSourceType.Quest, "2.0"),
        View(2, "Fat Chocobo", false, UnlockSourceType.Store, "2.1", categories: "Premium"),
        View(3, "Magitek Armor", true, UnlockSourceType.Quest, "2.0"),
        View(4, "Nightmare", false, UnlockSourceType.Duty, "2.45", categories: "Trial"),
        View(5, "Aithon", false, UnlockSourceType.Duty, "2.5", pinned: true, categories: "Trial"),
        View(6, "Moogle", false, UnlockSourceType.Event, "7.2", categories: "Event"),
        View(7, "Mystery", false, UnlockSourceType.Unknown, null),
    ];

    [Fact]
    public void Obtainable_filter_hides_store_event_and_legacy_only_entries()
    {
        var result = EntryViewQuery.Filter(Sample, OwnershipFilter.All, AvailabilityFilter.Obtainable, null).Select(v => v.Entry.Id);

        Assert.Equal([1u, 3u, 4u, 5u, 7u], result);
    }

    [Fact]
    public void Not_obtainable_filter_is_the_complement()
    {
        var result = EntryViewQuery.Filter(Sample, OwnershipFilter.All, AvailabilityFilter.NotObtainable, null).Select(v => v.Entry.Id);

        Assert.Equal([2u, 6u], result);
    }

    [Fact]
    public void Mixed_sources_count_as_obtainable_and_legacy_text_counts_as_closed()
    {
        var mixed = new UnlockSource(UnlockSourceType.Store, "s", Categories: ["Premium", "Trial"], Details: ["Premium: shop", "Trial: boss"]);
        var legacy = new UnlockSource(UnlockSourceType.Other, "s", Categories: ["Other"], Details: ["Other: Legacy Campaign"]);

        Assert.True(UnlockAvailability.IsObtainable(mixed));
        Assert.False(UnlockAvailability.IsObtainable(legacy));
        Assert.True(UnlockAvailability.IsObtainable(UnlockSource.Unknown()));
    }

    [Fact]
    public void Filters_and_search_combine()
    {
        var result = EntryViewQuery.Filter(Sample, OwnershipFilter.Missing, AvailabilityFilter.Obtainable, "a").Select(v => v.Entry.Id);

        Assert.Equal([4u, 5u], result);
    }

    [Fact]
    public void Patch_filter_keeps_only_that_patch_and_treats_equivalent_spellings_alike()
    {
        var result = EntryViewQuery.Filter(Sample, OwnershipFilter.All, AvailabilityFilter.All, null, onlyPatch: "2.0").Select(v => v.Entry.Id);

        Assert.Equal([1u, 3u], result);
        Assert.True(EntryViewQuery.IsPatch("7.20", "7.2"));
        Assert.False(EntryViewQuery.IsPatch(null, "7.2"));
        Assert.Empty(EntryViewQuery.Filter(Sample, OwnershipFilter.All, AvailabilityFilter.All, null, onlyPatch: "9.9"));
    }

    [Fact]
    public void Sort_by_name_is_case_insensitive_and_reversible()
    {
        var asc = EntryViewQuery.Sort(Sample, EntrySortKey.Name, false, false, false).Select(v => v.Entry.Name).ToArray();
        var desc = EntryViewQuery.Sort(Sample, EntrySortKey.Name, true, false, false).Select(v => v.Entry.Name).ToArray();

        Assert.Equal(["Aithon", "Company Chocobo", "Fat Chocobo", "Magitek Armor", "Moogle", "Mystery", "Nightmare"], asc);
        Assert.Equal(asc.Reverse(), desc);
    }

    [Fact]
    public void Sort_by_patch_is_numeric_with_unknown_last_and_stable()
    {
        var result = EntryViewQuery.Sort(Sample, EntrySortKey.Patch, false, false, false).Select(v => v.Entry.Id);

        Assert.Equal([1u, 3u, 2u, 4u, 5u, 6u, 7u], result);
        Assert.True(EntryViewQuery.ComparePatch("2.45", "2.5") < 0);
        Assert.True(EntryViewQuery.ComparePatch("7.2", "2.55") > 0);
    }

    [Fact]
    public void Pinned_then_missing_first_override_the_key()
    {
        var result = EntryViewQuery.Sort(Sample, EntrySortKey.Name, false, missingFirst: true, pinnedFirst: true).Select(v => v.Entry.Id);

        // Aithon pinned first; then missing by name: Fat Chocobo, Moogle, Mystery, Nightmare; then owned by name.
        Assert.Equal([5u, 2u, 6u, 7u, 4u, 1u, 3u], result);
    }

    [Fact]
    public void Default_sort_keeps_module_order()
    {
        var result = EntryViewQuery.Sort(Sample, EntrySortKey.Default, false, false, false).Select(v => v.Entry.Id);

        Assert.Equal([1u, 2u, 3u, 4u, 5u, 6u, 7u], result);
    }
}
