using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using NekoDex.Core.Checklist;
using NekoDex.Core.Outfits;
using NekoDex.Core.Unlock;

namespace NekoDex;

/// <summary>
/// Persisted settings. Add new fields with defaults freely; bump <see cref="CurrentVersion"/> and add a step
/// in <see cref="ConfigurationMigrator"/> only when an existing field changes meaning or shape.
/// </summary>
[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>Open the main window automatically when a character logs in.</summary>
    public bool OpenMainWindowOnLogin { get; set; }

    /// <summary>Which website the "open on web" buttons point at.</summary>
    public UnlockLinkSite LinkSite { get; set; } = UnlockLinkSite.FfxivCollect;

    /// <summary>User-defined daily/weekly to-dos.</summary>
    public List<ChecklistItem> Checklist { get; set; } = new();

    /// <summary>Whether the compact countdown/checklist window is shown.</summary>
    public bool ShowMiniWindow { get; set; }

    /// <summary>Neko look on, or plain Dalamud styling.</summary>
    public bool UseNekoTheme { get; set; } = true;

    /// <summary>Planned outfits.</summary>
    public List<Outfit> Outfits { get; set; } = new();

    /// <summary>Item ids last seen in the Glamour Dresser, kept because the game only exposes them after the dresser is opened.</summary>
    public List<uint> GlamourDresserItems { get; set; } = new();

    public DateTimeOffset? GlamourDresserCapturedAt { get; set; }
}
