namespace NekoDex.Core.Unlock;

/// <summary>How a collectable is obtained. <see cref="Unknown"/> is a real state the UI must render, never a blank.</summary>
public enum UnlockSourceType
{
    Unknown = 0,
    Quest,
    /// <summary>Dungeon, trial or raid drop.</summary>
    Duty,
    Vendor,
    Achievement,
    Event,
    Store,
    Other,

    /// <summary>Nothing in current game data grants it: no item, no source. Promotional or legacy rewards. Never obtainable.</summary>
    Legacy,
}
