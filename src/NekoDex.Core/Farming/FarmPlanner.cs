using NekoDex.Core.Collections;

namespace NekoDex.Core.Farming;

/// <summary>Missing entries that drop from one duty.</summary>
public sealed record FarmGroup(DutyInfo Duty, IReadOnlyList<EntryView> Entries);

/// <summary>Groups missing entries by the duties their unlock data names. Pure; the duty lookup is injected.</summary>
public static class FarmPlanner
{
    /// <summary>
    /// One group per duty that at least one missing entry drops from, ordered by difficulty then name.
    /// An entry that drops from several duties appears under each. Entries without duty data are skipped.
    /// </summary>
    public static IReadOnlyList<FarmGroup> Group(IEnumerable<EntryView> views, Func<uint, DutyInfo?> duty, bool hideHighEnd)
    {
        var buckets = new Dictionary<uint, (DutyInfo Duty, List<EntryView> Entries)>();
        foreach (var view in views)
        {
            if (view.Owned || view.Source.DutyIds is not { Count: > 0 } dutyIds)
            {
                continue;
            }

            foreach (var id in dutyIds)
            {
                if (duty(id) is not { } info || (hideHighEnd && DutyKindClassifier.IsHighEnd(info.Kind)))
                {
                    continue;
                }

                if (!buckets.TryGetValue(id, out var bucket))
                {
                    buckets[id] = bucket = (info, new List<EntryView>());
                }

                if (bucket.Entries.All(e => e.Entry.Id != view.Entry.Id))
                {
                    bucket.Entries.Add(view);
                }
            }
        }

        return buckets.Values
            .OrderBy(b => b.Duty.Kind)
            .ThenBy(b => b.Duty.Level)
            .ThenBy(b => b.Duty.Name, StringComparer.OrdinalIgnoreCase)
            .Select(b => new FarmGroup(b.Duty, b.Entries))
            .ToArray();
    }
}
