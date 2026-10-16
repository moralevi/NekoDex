namespace NekoDex.Core.Collections;

public enum OwnershipFilter
{
    All,
    Owned,
    Missing,
}

/// <summary>Filtering and searching over tracked entries. Pure, so the UI stays thin.</summary>
public static class EntryQuery
{
    /// <summary>
    /// Applies the ownership filter and a case-insensitive substring search on the entry name.
    /// A null or whitespace <paramref name="search"/> matches everything. Order is preserved.
    /// </summary>
    public static IEnumerable<TrackedEntry> Apply(IEnumerable<TrackedEntry> entries, OwnershipFilter filter, string? search)
    {
        if (!Enum.IsDefined(filter))
        {
            throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
        }

        var needle = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        return Iterate(entries, filter, needle);
    }

    private static IEnumerable<TrackedEntry> Iterate(IEnumerable<TrackedEntry> entries, OwnershipFilter filter, string? needle)
    {
        foreach (var tracked in entries)
        {
            if (!Matches(filter, tracked.Owned))
            {
                continue;
            }

            if (needle is not null && !tracked.Entry.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return tracked;
        }
    }

    private static bool Matches(OwnershipFilter filter, bool owned) => filter switch
    {
        OwnershipFilter.All => true,
        OwnershipFilter.Owned => owned,
        OwnershipFilter.Missing => !owned,
        _ => false,
    };
}
