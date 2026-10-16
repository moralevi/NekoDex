namespace NekoDex.Core.Collections;

/// <summary>A module-specific on/off filter shown next to the generic ones, e.g. "winnable from unlocked NPCs" for cards.</summary>
public sealed record EntryFilterOption(string Id, string Label, Func<CollectionEntry, bool> Predicate);

/// <summary>Modules that offer extra filters implement this next to <see cref="ICollectionModule"/>.</summary>
public interface IFilteredModule
{
    IReadOnlyList<EntryFilterOption> Filters { get; }
}
