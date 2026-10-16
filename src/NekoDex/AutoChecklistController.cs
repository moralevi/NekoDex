using System;
using System.Collections.Generic;
using NekoDex.Core.Checklist;
using NekoDex.Data;

namespace NekoDex;

/// <summary>Keeps auto item readings fresh while a character is loaded and exposes the enabled ones to the windows.</summary>
public sealed class AutoChecklistController
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly ConfigurationManager config;
    private readonly AutoChecklistSources sources;
    private readonly Func<bool> characterReady;
    private readonly Func<DateTimeOffset> clock;
    private readonly AutoItemTracker tracker = new();
    private DateTime lastRefresh = DateTime.MinValue;

    public AutoChecklistController(ConfigurationManager config, AutoChecklistSources sources, Func<bool> characterReady, Func<DateTimeOffset> clock)
    {
        this.config = config;
        this.sources = sources;
        this.characterReady = characterReady;
        this.clock = clock;
    }

    public IReadOnlyList<AutoItemDefinition> Definitions => sources.Definitions;

    public bool IsEnabled(string id) => config.Current.EnabledAutoItems.Contains(id);

    public void SetEnabled(string id, bool enabled)
    {
        var list = config.Current.EnabledAutoItems;
        if (enabled ? !list.Contains(id) : list.Remove(id))
        {
            if (enabled)
            {
                list.Add(id);
            }

            config.Save();
        }
    }

    /// <summary>Called on logout so one character's readings never show for another.</summary>
    public void Reset()
    {
        tracker.Clear();
        lastRefresh = DateTime.MinValue;
    }

    /// <summary>Enabled items with their latest reading (null when nothing has been read since the last reset).</summary>
    public IEnumerable<(AutoItemDefinition Definition, AutoItemReading? Reading)> Visible()
    {
        Refresh();
        var now = clock();
        foreach (var definition in sources.Definitions)
        {
            if (IsEnabled(definition.Id))
            {
                yield return (definition, tracker.Current(definition.Id, definition.Cadence, now));
            }
        }
    }

    private void Refresh()
    {
        var now = DateTime.UtcNow;
        if (now - lastRefresh < RefreshInterval || !characterReady())
        {
            return;
        }

        lastRefresh = now;
        var stamp = clock();
        foreach (var definition in sources.Definitions)
        {
            if (!IsEnabled(definition.Id))
            {
                continue;
            }

            if (sources.Read(definition.Id) is { } reading)
            {
                tracker.Observe(definition.Id, reading, stamp);
            }
        }
    }
}
