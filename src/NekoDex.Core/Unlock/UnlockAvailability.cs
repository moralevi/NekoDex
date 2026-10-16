namespace NekoDex.Core.Unlock;

public enum AvailabilityFilter
{
    All = 0,
    Obtainable = 1,
    NotObtainable = 2,
}

/// <summary>
/// Whether something can still be earned in game. Store-only (Premium), past events and legacy rewards count as not obtainable;
/// an entry with at least one other source counts as obtainable; no data at all counts as obtainable (benefit of the doubt).
/// </summary>
public static class UnlockAvailability
{
    private static readonly HashSet<string> ClosedCategories = new(StringComparer.OrdinalIgnoreCase) { "Premium", "Event" };

    public static bool IsObtainable(UnlockSource source)
    {
        if (source.IsUnknown)
        {
            return true;
        }

        var categories = source.Categories;
        if (categories is null || categories.Count == 0)
        {
            // Hand-written or game-data sources carry only a coarse type.
            return source.Type is not (UnlockSourceType.Store or UnlockSourceType.Event);
        }

        for (var i = 0; i < categories.Count; i++)
        {
            if (!IsClosed(categories[i], source.Details is { } details && i < details.Count ? details[i] : null))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Matches(AvailabilityFilter filter, UnlockSource source) => filter switch
    {
        AvailabilityFilter.Obtainable => IsObtainable(source),
        AvailabilityFilter.NotObtainable => !IsObtainable(source),
        _ => true,
    };

    private static bool IsClosed(string category, string? detail)
    {
        if (ClosedCategories.Contains(category))
        {
            return true;
        }

        // "Other: Legacy Campaign" and similar 1.0-era rewards.
        return detail is not null && detail.Contains("Legacy", StringComparison.OrdinalIgnoreCase);
    }
}
