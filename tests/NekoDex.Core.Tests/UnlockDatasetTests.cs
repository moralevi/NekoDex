using NekoDex.Core.Collections;
using NekoDex.Core.Unlock;

namespace NekoDex.Core.Tests;

public class UnlockDatasetTests
{
    private const string SampleJson = """
        {
          "schemaVersion": 1,
          "dataVersion": "2026-10-16",
          "gameVersion": "2026.10.06.0000.0000",
          "source": "FFXIV Collect",
          "modules": {
            "mounts": {
              "maxEntryId": 450,
              "entries": {
                "1": { "patch": "2.0", "sources": [
                  { "category": "Quest", "text": "My Little Chocobo (Immortal Flames)" },
                  { "category": "Quest", "text": "My Little Chocobo (Maelstrom)" } ] },
                "18": { "patch": "2.1", "sources": [ { "category": "Achievement", "text": "Jonathas - Old Gridania" } ] },
                "99": { "sources": [ { "category": "Moon Cheese", "text": "???" } ] }
              }
            }
          }
        }
        """;

    private static DatasetUnlockSourceProvider Provider() => new(UnlockDataset.Parse(SampleJson));

    [Fact]
    public void Parses_and_round_trips()
    {
        var dataset = UnlockDataset.Parse(SampleJson);

        Assert.Equal("2026-10-16", dataset.DataVersion);
        Assert.Equal(450u, dataset.Modules["mounts"].MaxEntryId);
        Assert.Equal(2, dataset.Modules["mounts"].Entries[1].Sources.Count);

        var again = UnlockDataset.Parse(dataset.ToJson());
        Assert.Equal(dataset.Modules["mounts"].Entries[18].Sources[0].Text, again.Modules["mounts"].Entries[18].Sources[0].Text);
    }

    [Fact]
    public void Rejects_other_schema_versions()
    {
        Assert.Throws<InvalidDataException>(() => UnlockDataset.Parse("""{ "schemaVersion": 99, "modules": {} }"""));
    }

    [Fact]
    public void Known_entry_maps_type_summary_details_patch_and_note()
    {
        var source = Provider().GetSource("mounts", new CollectionEntry(1, "Company Chocobo", 0));

        Assert.NotNull(source);
        Assert.Equal(UnlockSourceType.Quest, source.Type);
        Assert.Equal("My Little Chocobo (Immortal Flames) (+1)", source.Summary);
        Assert.Equal(["Quest: My Little Chocobo (Immortal Flames)", "Quest: My Little Chocobo (Maelstrom)"], source.Details);
        Assert.Equal("2.0", source.Patch);
        Assert.Equal("FFXIV Collect, data 2026-10-16", source.Note);
        Assert.Null(source.Url);
    }

    [Fact]
    public void Single_source_summary_is_the_text()
    {
        var source = Provider().GetSource("mounts", new CollectionEntry(18, "Behemoth", 0));

        Assert.Equal("Jonathas - Old Gridania", source!.Summary);
        Assert.Equal(UnlockSourceType.Achievement, source.Type);
    }

    [Fact]
    public void Unknown_category_becomes_other()
    {
        var source = Provider().GetSource("mounts", new CollectionEntry(99, "x", 0));

        Assert.Equal(UnlockSourceType.Other, source!.Type);
        Assert.False(UnlockCategories.IsKnown("Moon Cheese"));
    }

    [Fact]
    public void Entry_newer_than_data_is_explicit_unknown_with_note_and_gets_link_from_resolver()
    {
        var provider = Provider();
        var entry = new CollectionEntry(451, "Future Mount", 0);

        Assert.True(provider.IsNewerThanData("mounts", entry));
        var direct = provider.GetSource("mounts", entry);
        Assert.True(direct!.IsUnknown);
        Assert.Contains("2026-10-16", direct.Note);

        var resolved = new UnlockResolver([provider], () => UnlockLinkSite.FfxivCollect).Resolve("mounts", entry);
        Assert.True(resolved.IsUnknown);
        Assert.Equal("https://ffxivcollect.com/mounts/451", resolved.Url);
        Assert.Contains("2026-10-16", resolved.Note);
    }

    [Fact]
    public void Latest_patch_is_the_numerically_highest_and_null_without_data()
    {
        var provider = Provider();

        Assert.Equal("2.1", provider.LatestPatch("mounts"));
        Assert.Null(provider.LatestPatch("minions"));
    }

    [Fact]
    public void Missing_entry_within_range_and_unknown_module_return_null()
    {
        var provider = Provider();

        Assert.Null(provider.GetSource("mounts", new CollectionEntry(2, "Fat Chocobo", 0)));
        Assert.Null(provider.GetSource("minions", new CollectionEntry(1, "x", 0)));
        Assert.False(provider.IsNewerThanData("minions", new CollectionEntry(9999, "x", 0)));
    }
}
