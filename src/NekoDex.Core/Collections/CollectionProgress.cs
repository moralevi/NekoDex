namespace NekoDex.Core.Collections;

/// <summary>Owned-versus-total summary for one collection.</summary>
public readonly record struct CollectionProgress(int Owned, int Total)
{
    public int Missing => Total - Owned;

    /// <summary>0..1, and 0 when the collection is empty.</summary>
    public float Fraction => Total == 0 ? 0f : (float)Owned / Total;

    public static CollectionProgress From(IEnumerable<TrackedEntry> entries)
    {
        var owned = 0;
        var total = 0;
        foreach (var entry in entries)
        {
            total++;
            if (entry.Owned)
            {
                owned++;
            }
        }

        return new CollectionProgress(owned, total);
    }
}
