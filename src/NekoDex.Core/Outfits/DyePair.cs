namespace NekoDex.Core.Outfits;

/// <summary>Primary and secondary stain ids for one piece. 0 means undyed. Mutable and plain so it serialises into the config.</summary>
public sealed class DyePair
{
    public DyePair()
    {
    }

    public DyePair(uint primary, uint secondary)
    {
        Primary = primary;
        Secondary = secondary;
    }

    public uint Primary { get; set; }

    public uint Secondary { get; set; }

    public bool IsUndyed => Primary == 0 && Secondary == 0;

    public bool SameAs(DyePair other) => Primary == other.Primary && Secondary == other.Secondary;
}
