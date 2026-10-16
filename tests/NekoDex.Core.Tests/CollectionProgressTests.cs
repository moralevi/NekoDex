using NekoDex.Core.Collections;

namespace NekoDex.Core.Tests;

public class CollectionProgressTests
{
    [Fact]
    public void Counts_owned_and_total()
    {
        var entries = new TrackedEntry[]
        {
            new(new CollectionEntry(1, "a", 0), true),
            new(new CollectionEntry(2, "b", 0), false),
            new(new CollectionEntry(3, "c", 0), true),
        };

        var progress = CollectionProgress.From(entries);

        Assert.Equal(new CollectionProgress(Owned: 2, Total: 3), progress);
        Assert.Equal(1, progress.Missing);
        Assert.Equal(2f / 3f, progress.Fraction, precision: 5);
    }

    [Fact]
    public void Empty_collection_has_zero_fraction()
    {
        var progress = CollectionProgress.From([]);

        Assert.Equal(0, progress.Total);
        Assert.Equal(0f, progress.Fraction);
    }
}
