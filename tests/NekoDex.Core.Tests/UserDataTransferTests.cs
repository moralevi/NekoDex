using NekoDex.Core.Checklist;
using NekoDex.Core.Resets;
using NekoDex.Core.Transfer;

namespace NekoDex.Core.Tests;

public class UserDataTransferTests
{
    private static readonly DateTimeOffset When = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static UserDataState SampleState() => new()
    {
        Pins = new() { ["mounts"] = [18, 1] },
        Checklist = [new ChecklistItem { Text = "Turn in", Cadence = ResetCadence.Weekly, Checked = true, CheckedAtUtc = When }],
        EnabledAutoItems = ["roulette:5", "tribal"],
    };

    private static UserDataState EmptyState() => new() { Pins = new(), Checklist = [], EnabledAutoItems = [] };

    [Fact]
    public void Export_then_import_on_clean_state_reproduces_everything()
    {
        var source = SampleState();
        var json = UserDataTransfer.ToJson(UserDataTransfer.Export(source, "1.3.0.0", When));

        var file = UserDataTransfer.Parse(json);
        var target = EmptyState();
        var counts = UserDataTransfer.Apply(target, file, ImportMode.Merge);

        Assert.Equal(new SectionCounts(2, 1, 2), counts);
        Assert.Equal([18u, 1u], target.Pins["mounts"]);
        Assert.Equal(source.Checklist[0].Id, target.Checklist[0].Id);
        Assert.Equal(When, target.Checklist[0].CheckedAtUtc);
        Assert.Equal(["roulette:5", "tribal"], target.EnabledAutoItems);
        Assert.Equal("1.3.0.0", file.PluginVersion);
    }

    [Fact]
    public void Merge_keeps_local_on_id_clash_and_adds_the_rest()
    {
        var local = SampleState();
        var remote = SampleState();
        remote.Checklist[0].Id = local.Checklist[0].Id;
        remote.Checklist[0].Text = "Renamed remotely";
        remote.Checklist.Add(new ChecklistItem { Text = "Extra" });
        remote.Pins["mounts"].Add(99);
        remote.EnabledAutoItems.Add("bingo");

        var counts = UserDataTransfer.Apply(local, UserDataTransfer.Export(remote, "x", When), ImportMode.Merge);

        Assert.Equal(new SectionCounts(1, 1, 1), counts);
        Assert.Equal("Turn in", local.Checklist[0].Text);
        Assert.Equal(2, local.Checklist.Count);
        Assert.Contains(99u, local.Pins["mounts"]);
        Assert.Contains("bingo", local.EnabledAutoItems);
    }

    [Fact]
    public void Replace_overwrites_present_sections_and_leaves_absent_ones()
    {
        var local = SampleState();
        var file = new UserDataFile { Pins = new() { ["minions"] = [5] } };

        UserDataTransfer.Apply(local, file, ImportMode.Replace);

        Assert.Equal(["minions"], local.Pins.Keys);
        Assert.Single(local.Checklist);
        Assert.Equal(2, local.EnabledAutoItems.Count);
    }

    [Fact]
    public void Older_export_with_removed_sections_still_imports_what_remains()
    {
        var json = """{ "schemaVersion": 1, "pins": { "mounts": [7] }, "outfits": [ { "name": "gone" } ], "dresserSnapshots": {} }""";

        var file = UserDataTransfer.Parse(json);
        var target = EmptyState();

        Assert.Equal(new SectionCounts(1, 0, 0), UserDataTransfer.Apply(target, file, ImportMode.Merge));
    }

    [Fact]
    public void Unknown_schema_and_garbage_are_refused()
    {
        Assert.Throws<InvalidDataException>(() => UserDataTransfer.Parse("""{ "schemaVersion": 42 }"""));
        Assert.Throws<InvalidDataException>(() => UserDataTransfer.Parse("not json"));
    }

    [Fact]
    public void Preview_counts_sections()
    {
        var file = UserDataTransfer.Export(SampleState(), "x", When);

        Assert.Equal(new SectionCounts(2, 1, 2), UserDataTransfer.Preview(file));
        Assert.Equal(5, UserDataTransfer.Preview(file).Total);
    }
}
