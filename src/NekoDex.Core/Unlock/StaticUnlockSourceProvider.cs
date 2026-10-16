using NekoDex.Core.Collections;

namespace NekoDex.Core.Unlock;

/// <summary>In-memory provider keyed by module id and entry id. Used for hand-verified entries and in tests.</summary>
public sealed class StaticUnlockSourceProvider : IUnlockSourceProvider
{
    private readonly Dictionary<(string ModuleId, uint EntryId), UnlockSource> sources = new();

    public StaticUnlockSourceProvider Add(string moduleId, uint entryId, UnlockSource source)
    {
        sources[(moduleId, entryId)] = source;
        return this;
    }

    public int Count => sources.Count;

    public UnlockSource? GetSource(string moduleId, CollectionEntry entry)
        => sources.TryGetValue((moduleId, entry.Id), out var source) ? source : null;
}
