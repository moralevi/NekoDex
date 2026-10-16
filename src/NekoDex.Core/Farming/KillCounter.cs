namespace NekoDex.Core.Farming;

/// <summary>
/// Duty completions per character per duty. Every completed duty is counted (decided 2026-10-16); the UI shows the count
/// only where a duty matters. Plain dictionaries so it serialises into the config.
/// </summary>
public sealed class KillCounter
{
    public Dictionary<ulong, Dictionary<uint, int>> Clears { get; set; } = new();

    public int Count(ulong contentId, uint dutyId)
        => Clears.TryGetValue(contentId, out var byDuty) && byDuty.TryGetValue(dutyId, out var n) ? n : 0;

    /// <summary>Returns the new count.</summary>
    public int Record(ulong contentId, uint dutyId)
    {
        if (contentId == 0 || dutyId == 0)
        {
            return 0;
        }

        if (!Clears.TryGetValue(contentId, out var byDuty))
        {
            Clears[contentId] = byDuty = new Dictionary<uint, int>();
        }

        byDuty[dutyId] = byDuty.GetValueOrDefault(dutyId) + 1;
        return byDuty[dutyId];
    }

    /// <summary>Returns true when there was something to reset.</summary>
    public bool Reset(ulong contentId, uint dutyId)
    {
        if (!Clears.TryGetValue(contentId, out var byDuty) || !byDuty.Remove(dutyId))
        {
            return false;
        }

        if (byDuty.Count == 0)
        {
            Clears.Remove(contentId);
        }

        return true;
    }
}
