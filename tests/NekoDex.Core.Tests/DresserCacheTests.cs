using NekoDex.Core.Dresser;

namespace NekoDex.Core.Tests;

public class DresserCacheTests
{
    private static readonly DateTimeOffset When = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Legacy_snapshot_goes_to_first_character_seen()
    {
        var cache = new DresserCache { Legacy = new DresserSnapshot { Items = [1, 2, 3], CapturedAt = When } };

        Assert.True(cache.AdoptLegacy(1001, "Alice"));

        var adopted = cache.Get(1001);
        Assert.NotNull(adopted);
        Assert.Equal([1u, 2u, 3u], adopted.Items);
        Assert.Equal(When, adopted.CapturedAt);
        Assert.Equal("Alice", adopted.CharacterName);
        Assert.Null(cache.Legacy);
        Assert.Null(cache.Get(1002));
    }

    [Fact]
    public void Legacy_is_discarded_when_character_already_has_a_snapshot()
    {
        var cache = new DresserCache { Legacy = new DresserSnapshot { Items = [9], CapturedAt = When } };
        cache.Set(1001, [5], When.AddHours(1), "Alice");

        Assert.True(cache.AdoptLegacy(1001, "Alice"));

        Assert.Equal([5u], cache.Get(1001)!.Items);
        Assert.Null(cache.Legacy);
    }

    [Fact]
    public void Adopt_without_legacy_changes_nothing()
    {
        var cache = new DresserCache();

        Assert.False(cache.AdoptLegacy(1001, "Alice"));
        Assert.Empty(cache.Snapshots);
    }

    [Fact]
    public void Characters_are_isolated_and_set_replaces()
    {
        var cache = new DresserCache();
        cache.Set(1001, [1, 2], When, "Alice");
        cache.Set(1002, [3], When, "Bob");
        cache.Set(1001, [7], When.AddDays(1), "Alice");

        Assert.Equal([7u], cache.Get(1001)!.Items);
        Assert.Equal([3u], cache.Get(1002)!.Items);
        Assert.Equal(When.AddDays(1), cache.Get(1001)!.CapturedAt);
    }

    [Fact]
    public void Forget_removes_only_that_character()
    {
        var cache = new DresserCache();
        cache.Set(1001, [1], When, "Alice");
        cache.Set(1002, [2], When, "Bob");

        Assert.True(cache.Forget(1001));
        Assert.False(cache.Forget(1001));
        Assert.Null(cache.Get(1001));
        Assert.NotNull(cache.Get(1002));
    }
}
