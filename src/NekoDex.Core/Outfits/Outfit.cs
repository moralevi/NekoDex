namespace NekoDex.Core.Outfits;

/// <summary>A named set of item ids per slot, with optional dyes. Mutable and plain so it serialises straight into the plugin configuration.</summary>
public sealed class Outfit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Item id per slot. Absent slot means "nothing planned".</summary>
    public Dictionary<GearSlot, uint> Items { get; set; } = new();

    /// <summary>Dyes per slot. Absent means undyed.</summary>
    public Dictionary<GearSlot, DyePair> Dyes { get; set; } = new();

    public DyePair DyeFor(GearSlot slot) => Dyes.TryGetValue(slot, out var dye) ? dye : new DyePair();

    public void SetDye(GearSlot slot, DyePair dye)
    {
        if (dye.IsUndyed)
        {
            Dyes.Remove(slot);
        }
        else
        {
            Dyes[slot] = dye;
        }
    }

    /// <summary>Replaces every slot with the given items and dyes, clearing slots that are absent or 0.</summary>
    public void Fill(IReadOnlyDictionary<GearSlot, uint> items, IReadOnlyDictionary<GearSlot, DyePair>? dyes)
    {
        Items.Clear();
        Dyes.Clear();
        foreach (var (slot, itemId) in items)
        {
            if (itemId == 0)
            {
                continue;
            }

            Items[slot] = itemId;
            if (dyes is not null && dyes.TryGetValue(slot, out var dye))
            {
                SetDye(slot, dye);
            }
        }
    }
}
