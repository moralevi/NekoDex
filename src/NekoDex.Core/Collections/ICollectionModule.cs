namespace NekoDex.Core.Collections;

/// <summary>
/// A collection the tracker can display: mounts, minions, orchestrion rolls, ...
/// Implementations live in the plugin's Data layer because they read game data;
/// this interface stays free of any game dependency so the UI and tests can work against fakes.
/// </summary>
public interface ICollectionModule
{
    /// <summary>Stable identifier used for config keys and logging, e.g. "mounts".</summary>
    string Id { get; }

    /// <summary>Human-readable name shown in tabs and headers.</summary>
    string DisplayName { get; }

    /// <summary>All entries in this collection, regardless of ownership. Must be cheap to call every frame (cache internally).</summary>
    IReadOnlyList<CollectionEntry> GetEntries();

    /// <summary>Whether the logged-in character owns <paramref name="entry"/>. Only valid while a character is loaded.</summary>
    bool IsOwned(CollectionEntry entry);
}
