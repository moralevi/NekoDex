using NekoDex.Core.Resets;

namespace NekoDex.Core.Checklist;

/// <summary>A user-defined to-do. Mutable and plain so it serialises straight into the plugin configuration.</summary>
public sealed class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;

    public ResetCadence Cadence { get; set; } = ResetCadence.Daily;

    public bool Checked { get; set; }

    /// <summary>When it was last ticked, so a reset that happened after can clear it.</summary>
    public DateTimeOffset? CheckedAtUtc { get; set; }
}
