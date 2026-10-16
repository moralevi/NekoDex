namespace NekoDex.Core.Resets;

/// <summary>
/// Notices when a reset passes between two ticks. The first tick after construction or <see cref="Clear"/> only primes,
/// so logging in just after a reset never announces it.
/// </summary>
public sealed class ResetWatcher
{
    private readonly Dictionary<string, DateTimeOffset> next = new();

    /// <summary>Returns the resets whose boundary was crossed since the previous tick. Unknown resets are primed silently.</summary>
    public IReadOnlyList<ResetDefinition> Tick(IEnumerable<ResetDefinition> resets, DateTimeOffset nowUtc)
    {
        var crossed = new List<ResetDefinition>();
        foreach (var reset in resets)
        {
            var upcoming = reset.NextAfter(nowUtc);
            if (next.TryGetValue(reset.Id, out var expected) && nowUtc >= expected)
            {
                crossed.Add(reset);
            }

            next[reset.Id] = upcoming;
        }

        return crossed;
    }

    public void Clear() => next.Clear();
}
