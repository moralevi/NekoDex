using NekoDex.Core.Collections;

namespace NekoDex.Core.Tests;

public class CollectionSnapshotTests
{
    private sealed class FakeModule(IReadOnlyList<CollectionEntry> entries, Func<CollectionEntry, bool> owned) : ICollectionModule
    {
        public string Id => "fake";

        public string DisplayName => "Fake";

        public string? UnavailableReason => null;

        public IReadOnlyList<CollectionEntry> GetEntries() => entries;

        public bool IsOwned(CollectionEntry entry) => owned(entry);
    }

    [Fact]
    public void Build_pairs_entries_with_ownership_and_counts()
    {
        var entries = new[]
        {
            new CollectionEntry(1, "a", 0),
            new CollectionEntry(2, "b", 0),
            new CollectionEntry(3, "c", 0),
        };
        var module = new FakeModule(entries, e => e.Id != 2);

        var snapshot = CollectionSnapshot.Build(module);

        Assert.Equal([true, false, true], snapshot.Entries.Select(e => e.Owned));
        Assert.Equal(new CollectionProgress(2, 3), snapshot.Progress);
    }

    [Fact]
    public void Empty_snapshot_has_no_entries()
    {
        Assert.Empty(CollectionSnapshot.Empty.Entries);
        Assert.Equal(0, CollectionSnapshot.Empty.Progress.Total);
    }
}
