namespace NekoDex.Core.Collections;

/// <summary>
/// A collection the tracker can display: mounts, minions, orchestrion rolls, ...
/// Implementations live in the plugin's Data layer because they read game data;
/// this interface stays free of any game dependency so the UI and tests can work against fakes.
/// </summary>
public interface ICollectionModule
{
    /// <summary>Stable identifier used for config keys, dataset keys and logging, e.g. "mounts".</summary>
    string Id { get; }

    /// <summary>Human-readable name shown in tabs and headers.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Null when ownership can be read right now; otherwise a sentence telling the user what to do
    /// (e.g. "Open the Armoire once this session"). Entries may still be listed while unavailable.
    /// </summary>
    string? UnavailableReason { get; }

    /// <summary>All entries in this collection, regardless of ownership. Must be cheap to call every frame (cache internally).</summary>
    IReadOnlyList<CollectionEntry> GetEntries();

    /// <summary>Whether the logged-in character owns <paramref name="entry"/>. Only meaningful while <see cref="UnavailableReason"/> is null.</summary>
    bool IsOwned(CollectionEntry entry);
}
