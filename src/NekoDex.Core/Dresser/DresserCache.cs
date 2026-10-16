using NekoDex.Core.Outfits;

namespace NekoDex.Core.Dresser;

/// <summary>What one character had in the Glamour Dresser the last time the game exposed it.</summary>
public sealed class DresserSnapshot
{
    public List<uint> Items { get; set; } = new();

    /// <summary>Dyes seen per item id; the same item can sit in the dresser in several dyes.</summary>
    public Dictionary<uint, List<DyePair>> Dyes { get; set; } = new();

    public DateTimeOffset CapturedAt { get; set; }

    /// <summary>For the settings UI only; the key is the content id.</summary>
    public string? CharacterName { get; set; }

    /// <summary>True when the item is in the dresser in exactly this dye. An undyed request matches any copy of the item.</summary>
    public bool HasDye(uint itemId, DyePair dye)
    {
        if (!Items.Contains(itemId))
        {
            return false;
        }

        if (dye.IsUndyed)
        {
            return true;
        }

        return Dyes.TryGetValue(itemId, out var seen) && seen.Any(d => d.SameAs(dye));
    }
}

/// <summary>
/// Glamour Dresser snapshots per character, keyed by content id. Plain and mutable so it serialises into the plugin config.
/// <see cref="Legacy"/> holds a snapshot from before per-character storage existed; the first character seen adopts it.
/// </summary>
public sealed class DresserCache
{
    public Dictionary<ulong, DresserSnapshot> Snapshots { get; set; } = new();

    public DresserSnapshot? Legacy { get; set; }

    public DresserSnapshot? Get(ulong contentId)
        => Snapshots.TryGetValue(contentId, out var snapshot) ? snapshot : null;

    /// <summary>Stores or replaces the snapshot for a character.</summary>
    public void Set(ulong contentId, IEnumerable<uint> items, DateTimeOffset capturedAt, string? characterName, Dictionary<uint, List<DyePair>>? dyes = null)
    {
        Snapshots[contentId] = new DresserSnapshot
        {
            Items = new List<uint>(items),
            Dyes = dyes ?? new Dictionary<uint, List<DyePair>>(),
            CapturedAt = capturedAt,
            CharacterName = characterName,
        };
    }

    /// <summary>
    /// Gives the pre-v2 snapshot to <paramref name="contentId"/> when that character has none yet; otherwise discards it,
    /// because a fresh per-character read is better than guessing. Returns true when anything changed.
    /// </summary>
    public bool AdoptLegacy(ulong contentId, string? characterName)
    {
        if (Legacy is null)
        {
            return false;
        }

        if (!Snapshots.ContainsKey(contentId))
        {
            Legacy.CharacterName = characterName;
            Snapshots[contentId] = Legacy;
        }

        Legacy = null;
        return true;
    }

    public bool Forget(ulong contentId) => Snapshots.Remove(contentId);
}
