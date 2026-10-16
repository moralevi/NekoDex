using NekoDex.Core.Resets;

namespace NekoDex.Core.Checklist;

/// <summary>A checklist item the game reports itself. The Data layer supplies readings; the UI never lets the user tick it.</summary>
public sealed record AutoItemDefinition(string Id, string Name, ResetCadence Cadence);

/// <summary>One observation: done or not, plus an optional detail such as "3 left" or "7/9 stickers".</summary>
public sealed record AutoItemReading(bool Complete, string? Detail = null);

/// <summary>
/// Remembers the last reading per item so the list stays meaningful when the game cannot be read (login screen, loading).
/// A reading older than the item's last reset is stale and no longer reported.
/// </summary>
public sealed class AutoItemTracker
{
    private readonly Dictionary<string, (AutoItemReading Reading, DateTimeOffset At)> last = new();

    public void Observe(string id, AutoItemReading reading, DateTimeOffset nowUtc)
        => last[id] = (reading, nowUtc.ToUniversalTime());

    /// <summary>The last reading, or null when there is none or a reset has happened since it was taken.</summary>
    public AutoItemReading? Current(string id, ResetCadence cadence, DateTimeOffset nowUtc)
    {
        if (!last.TryGetValue(id, out var entry))
        {
            return null;
        }

        var reset = KnownResets.For(cadence);
        if (reset is not null && reset.LastAtOrBefore(nowUtc) > entry.At)
        {
            last.Remove(id);
            return null;
        }

        return entry.Reading;
    }

    public void Clear() => last.Clear();
}
