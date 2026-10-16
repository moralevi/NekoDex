namespace NekoDex.Core.Collections;

/// <summary>
/// Ownership of every entry in a module at one moment, plus the derived progress.
/// Built on demand so the UI never queries the game per frame.
/// </summary>
public sealed class CollectionSnapshot
{
    private CollectionSnapshot(IReadOnlyList<TrackedEntry> entries, CollectionProgress progress)
    {
        Entries = entries;
        Progress = progress;
    }

    public static CollectionSnapshot Empty { get; } = new([], new CollectionProgress(0, 0));

    public IReadOnlyList<TrackedEntry> Entries { get; }

    public CollectionProgress Progress { get; }

    public static CollectionSnapshot Build(ICollectionModule module)
    {
        var source = module.GetEntries();
        var entries = new TrackedEntry[source.Count];
        var owned = 0;
        for (var i = 0; i < source.Count; i++)
        {
            var isOwned = module.IsOwned(source[i]);
            entries[i] = new TrackedEntry(source[i], isOwned);
            if (isOwned)
            {
                owned++;
            }
        }

        return new CollectionSnapshot(entries, new CollectionProgress(owned, entries.Length));
    }
}
