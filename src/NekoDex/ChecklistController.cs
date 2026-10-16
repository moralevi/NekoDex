using System;
using System.Collections.Generic;
using NekoDex.Core.Checklist;
using NekoDex.Core.Resets;

namespace NekoDex;

/// <summary>Owns the checklist stored in the configuration: applies resets, edits items and saves. Shared by every window that shows it.</summary>
public sealed class ChecklistController
{
    private readonly ConfigurationManager config;
    private readonly Func<DateTimeOffset> clock;

    public ChecklistController(ConfigurationManager config, Func<DateTimeOffset> clock)
    {
        this.config = config;
        this.clock = clock;
    }

    public IReadOnlyList<ChecklistItem> Items => config.Current.Checklist;

    public DateTimeOffset NowUtc => clock().ToUniversalTime();

    /// <summary>Clear items whose reset has passed. Cheap; call once per frame from any window that shows the list.</summary>
    public void ApplyResets()
    {
        if (ChecklistService.ApplyResets(config.Current.Checklist, NowUtc))
        {
            config.Save();
        }
    }

    public void SetChecked(ChecklistItem item, bool isChecked)
    {
        if (ChecklistService.SetChecked(item, isChecked, NowUtc))
        {
            config.Save();
        }
    }

    public void Add(string text, ResetCadence cadence)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        config.Current.Checklist.Add(new ChecklistItem { Text = trimmed, Cadence = cadence });
        config.Save();
    }

    public void Remove(ChecklistItem item)
    {
        if (config.Current.Checklist.Remove(item))
        {
            config.Save();
        }
    }
}
