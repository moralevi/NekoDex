using NekoDex.Core.Resets;

namespace NekoDex.Core.Checklist;

/// <summary>Checklist rules: ticking records the time, and a reset after that time clears the tick. Pure; the caller persists.</summary>
public static class ChecklistService
{
    /// <summary>Sets the checked state and stamps the time. Returns true when something changed.</summary>
    public static bool SetChecked(ChecklistItem item, bool isChecked, DateTimeOffset nowUtc)
    {
        if (item.Checked == isChecked)
        {
            return false;
        }

        item.Checked = isChecked;
        item.CheckedAtUtc = isChecked ? nowUtc.ToUniversalTime() : null;
        return true;
    }

    /// <summary>Clears every item whose cadence has reset since it was ticked. Returns true when any item changed.</summary>
    public static bool ApplyResets(IEnumerable<ChecklistItem> items, DateTimeOffset nowUtc)
    {
        var changed = false;
        foreach (var item in items)
        {
            if (ShouldClear(item, nowUtc))
            {
                item.Checked = false;
                item.CheckedAtUtc = null;
                changed = true;
            }
        }

        return changed;
    }

    public static bool ShouldClear(ChecklistItem item, DateTimeOffset nowUtc)
    {
        if (!item.Checked)
        {
            return false;
        }

        var reset = KnownResets.For(item.Cadence);
        if (reset is null)
        {
            return false;
        }

        // No timestamp (older config) means we cannot prove it survived a reset: clear it.
        if (item.CheckedAtUtc is not { } checkedAt)
        {
            return true;
        }

        return reset.LastAtOrBefore(nowUtc) > checkedAt.ToUniversalTime();
    }
}
