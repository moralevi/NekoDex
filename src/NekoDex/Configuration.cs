using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using NekoDex.Core.Checklist;
using NekoDex.Core.Farming;
using NekoDex.Core.Unlock;

namespace NekoDex;

/// <summary>
/// Persisted settings. Add new fields with defaults freely; bump <see cref="CurrentVersion"/> and add a step
/// in <see cref="ConfigurationMigrator"/> only when an existing field changes meaning or shape.
/// Fields from removed features (outfits, dresser snapshots) are ignored on load and dropped on the next save.
/// </summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>Open the main window automatically when a character logs in.</summary>
    public bool OpenMainWindowOnLogin { get; set; }

    /// <summary>Which website the "open on web" buttons point at.</summary>
    public UnlockLinkSite LinkSite { get; set; } = UnlockLinkSite.FfxivCollect;

    /// <summary>User-defined daily/weekly to-dos.</summary>
    public List<ChecklistItem> Checklist { get; set; } = new();

    /// <summary>Ids of game-reported items shown in the checklist (see AutoChecklistSources).</summary>
    public List<string> EnabledAutoItems { get; set; } = new(Data.AutoChecklistSources.DefaultEnabled);

    /// <summary>Whether the compact countdown/checklist window is shown.</summary>
    public bool ShowMiniWindow { get; set; }

    /// <summary>Mini window keeps its position and size and hides its title bar.</summary>
    public bool MiniLocked { get; set; }

    /// <summary>Mini window ignores the mouse entirely.</summary>
    public bool MiniClickthrough { get; set; }

    /// <summary>Mini window background opacity, 0.2 to 1.</summary>
    public float MiniOpacity { get; set; } = 1f;

    /// <summary>Toast when the daily or weekly reset passes while playing.</summary>
    public bool ResetToasts { get; set; } = true;

    /// <summary>Neko look on, or plain Dalamud styling.</summary>
    public bool UseNekoTheme { get; set; } = true;

    /// <summary>Language code to use instead of Dalamud's UI language, e.g. "sv". Null follows Dalamud.</summary>
    public string? LanguageOverride { get; set; }

    /// <summary>Starred entries per module id ("mounts" -> mount row ids). Shown first in their tab and in the mini window.</summary>
    public Dictionary<string, List<uint>> Pins { get; set; } = new();

    /// <summary>Duty completions per character per ContentFinderCondition id, counted while NekoDex runs.</summary>
    public KillCounter DutyClears { get; set; } = new();
}
