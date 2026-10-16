namespace NekoDex.Core.Outfits;

/// <summary>A named set of item ids per slot. Mutable and plain so it serialises straight into the plugin configuration.</summary>
public sealed class Outfit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Item id per slot. Absent slot means "nothing planned".</summary>
    public Dictionary<GearSlot, uint> Items { get; set; } = new();
}
