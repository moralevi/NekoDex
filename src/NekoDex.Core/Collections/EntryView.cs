using NekoDex.Core.Unlock;

namespace NekoDex.Core.Collections;

/// <summary>A tracked entry with everything the table needs to filter and sort it.</summary>
public sealed record EntryView(TrackedEntry Tracked, UnlockSource Source, bool Pinned)
{
    public CollectionEntry Entry => Tracked.Entry;

    public bool Owned => Tracked.Owned;
}

public enum EntrySortKey
{
    Default = 0,
    Name = 1,
    Patch = 2,
    UnlockType = 3,
    Status = 4,
    Category = 5,
    Rank = 6,
}

/// <summary>Filtering and sorting over <see cref="EntryView"/>. Pure, so the UI stays thin and this is tested.</summary>
public static class EntryViewQuery
{
    /// <summary>
    /// Applies ownership, availability and search filters; when <paramref name="onlyPatch"/> is set, keeps entries added in exactly that patch.
    /// </summary>
    public static IEnumerable<EntryView> Filter(IEnumerable<EntryView> views, OwnershipFilter ownership, AvailabilityFilter availability, string? search, string? onlyPatch = null, Func<CollectionEntry, bool>? extra = null)
    {
        var needle = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        foreach (var view in views)
        {
            if (!OwnershipMatches(ownership, view.Owned))
            {
                continue;
            }

            if (onlyPatch is not null && !IsPatch(view.Source.Patch, onlyPatch))
            {
                continue;
            }

            if (extra is not null && !extra(view.Entry))
            {
                continue;
            }

            if (!UnlockAvailability.Matches(availability, view.Source))
            {
                continue;
            }

            if (needle is not null && !view.Entry.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return view;
        }
    }

    /// <summary>
    /// Stable sort. Pinned entries come first when <paramref name="pinnedFirst"/>; then missing before owned when
    /// <paramref name="missingFirst"/>; then the chosen key; ties keep the module's own order.
    /// </summary>
    public static IReadOnlyList<EntryView> Sort(IEnumerable<EntryView> views, EntrySortKey key, bool descending, bool missingFirst, bool pinnedFirst)
    {
        var list = views.Select((view, index) => (view, index)).ToList();
        list.Sort((a, b) =>
        {
            if (pinnedFirst && a.view.Pinned != b.view.Pinned)
            {
                return a.view.Pinned ? -1 : 1;
            }

            if (missingFirst && a.view.Owned != b.view.Owned)
            {
                return a.view.Owned ? 1 : -1;
            }

            var c = Compare(a.view, b.view, key);
            if (descending)
            {
                c = -c;
            }

            return c != 0 ? c : a.index.CompareTo(b.index);
        });

        return list.Select(x => x.view).ToArray();
    }

    private static int Compare(EntryView a, EntryView b, EntrySortKey key) => key switch
    {
        EntrySortKey.Name => string.Compare(a.Entry.Name, b.Entry.Name, StringComparison.OrdinalIgnoreCase),
        EntrySortKey.Patch => ComparePatch(a.Source.Patch, b.Source.Patch),
        EntrySortKey.UnlockType => ((int)a.Source.Type).CompareTo((int)b.Source.Type),
        EntrySortKey.Status => a.Owned.CompareTo(b.Owned),
        EntrySortKey.Category => string.Compare(a.Entry.Category ?? string.Empty, b.Entry.Category ?? string.Empty, StringComparison.OrdinalIgnoreCase),
        EntrySortKey.Rank => a.Entry.Rank.CompareTo(b.Entry.Rank),
        _ => 0,
    };

    /// <summary>True when both patches parse and denote the same version ("7.2" equals "7.20").</summary>
    public static bool IsPatch(string? patch, string wanted) => patch is not null && ParsePatch(patch) is { } a && ParsePatch(wanted) is { } b && a == b;

    /// <summary>"2.0" &lt; "2.55" &lt; "7.2"; unknown patches sort last.</summary>
    public static int ComparePatch(string? a, string? b)
    {
        var pa = ParsePatch(a);
        var pb = ParsePatch(b);
        if (pa is null || pb is null)
        {
            return pa is null ? (pb is null ? 0 : 1) : -1;
        }

        var c = pa.Value.Major.CompareTo(pb.Value.Major);
        return c != 0 ? c : pa.Value.Minor.CompareTo(pb.Value.Minor);
    }

    private static (int Major, int Minor)? ParsePatch(string? patch)
    {
        if (string.IsNullOrWhiteSpace(patch))
        {
            return null;
        }

        var parts = patch.Split('.', 2);
        if (!int.TryParse(parts[0], out var major))
        {
            return null;
        }

        // Minor digits are positional: "2.5" is 2.50 and sorts after "2.45".
        var minor = 0;
        if (parts.Length > 1 && parts[1].Length > 0)
        {
            var digits = parts[1].Length >= 2 ? parts[1][..2] : parts[1].PadRight(2, '0');
            if (!int.TryParse(digits, out minor))
            {
                return null;
            }
        }

        return (major, minor);
    }

    private static bool OwnershipMatches(OwnershipFilter filter, bool owned) => filter switch
    {
        OwnershipFilter.Owned => owned,
        OwnershipFilter.Missing => !owned,
        _ => true,
    };
}
