using NekoDex.Core.Checklist;
using NekoDex.Core.Dresser;
using NekoDex.Core.Outfits;
using NekoDex.Core.Resets;
using NekoDex.Core.Transfer;

namespace NekoDex.Core.Tests;

public class UserDataTransferTests
{
    private static readonly DateTimeOffset When = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static UserDataState SampleState()
    {
        var outfit = new Outfit { Name = "Casual", Items = { [GearSlot.Body] = 100, [GearSlot.Legs] = 200 }, Dyes = { [GearSlot.Body] = new DyePair(5, 6) } };
        var state = new UserDataState
        {
            Outfits = [outfit],
            Pins = new() { ["mounts"] = [18, 1] },
            Checklist = [new ChecklistItem { Text = "Turn in", Cadence = ResetCadence.Weekly, Checked = true, CheckedAtUtc = When }],
            EnabledAutoItems = ["roulette:5", "tribal"],
            DresserSnapshots = new() { [1001] = new DresserSnapshot { Items = [100], CapturedAt = When, CharacterName = "Alice", Dyes = { [100] = [new DyePair(5, 6)] } } },
        };
        return state;
    }

    private static UserDataState EmptyState() => new()
    {
        Outfits = [],
        Pins = new(),
        Checklist = [],
        EnabledAutoItems = [],
        DresserSnapshots = new(),
    };

    [Fact]
    public void Export_then_import_on_clean_state_reproduces_everything()
    {
        var source = SampleState();
        var json = UserDataTransfer.ToJson(UserDataTransfer.Export(source, "1.3.0.0", When, outfitsOnly: false));

        var file = UserDataTransfer.Parse(json);
        var target = EmptyState();
        var counts = UserDataTransfer.Apply(target, file, ImportMode.Merge);

        Assert.Equal(new SectionCounts(1, 2, 1, 2, 1), counts);
        var outfit = Assert.Single(target.Outfits);
        Assert.Equal(source.Outfits[0].Id, outfit.Id);
        Assert.Equal(200u, outfit.Items[GearSlot.Legs]);
        Assert.Equal(6u, outfit.DyeFor(GearSlot.Body).Secondary);
        Assert.Equal([18u, 1u], target.Pins["mounts"]);
        Assert.Equal(When, target.Checklist[0].CheckedAtUtc);
        Assert.Equal("Alice", target.DresserSnapshots[1001].CharacterName);
        Assert.True(target.DresserSnapshots[1001].HasDye(100, new DyePair(5, 6)));
        Assert.Equal("1.3.0.0", file.PluginVersion);
    }

    [Fact]
    public void Outfits_only_export_leaves_other_sections_untouched_on_import()
    {
        var json = UserDataTransfer.ToJson(UserDataTransfer.Export(SampleState(), "x", When, outfitsOnly: true));
        var file = UserDataTransfer.Parse(json);
        Assert.Null(file.Pins);
        Assert.Null(file.DresserSnapshots);

        var target = SampleState();
        target.Outfits.Clear();
        var counts = UserDataTransfer.Apply(target, file, ImportMode.Replace);

        Assert.Equal(new SectionCounts(1, 0, 0, 0, 0), counts);
        Assert.Single(target.Outfits);
        Assert.Single(target.Pins);
        Assert.Single(target.Checklist);
        Assert.Single(target.DresserSnapshots);
    }

    [Fact]
    public void Merge_keeps_local_on_id_clash_and_adds_the_rest()
    {
        var local = SampleState();
        var remote = SampleState();
        remote.Outfits[0].Id = local.Outfits[0].Id;
        remote.Checklist[0].Id = local.Checklist[0].Id;
        remote.Outfits[0].Name = "Renamed remotely";
        remote.Outfits.Add(new Outfit { Name = "Extra" });
        remote.Pins["mounts"].Add(99);
        remote.EnabledAutoItems.Add("bingo");

        var counts = UserDataTransfer.Apply(local, UserDataTransfer.Export(remote, "x", When, false), ImportMode.Merge);

        Assert.Equal(new SectionCounts(1, 1, 0, 1, 0), counts);
        Assert.Equal("Casual", local.Outfits[0].Name);
        Assert.Equal(2, local.Outfits.Count);
        Assert.Contains(99u, local.Pins["mounts"]);
        Assert.Contains("bingo", local.EnabledAutoItems);
    }

    [Fact]
    public void Replace_overwrites_present_sections()
    {
        var local = SampleState();
        local.Outfits.Add(new Outfit { Name = "Local only" });
        var remote = EmptyState();
        remote.Outfits.Add(new Outfit { Name = "Remote" });

        UserDataTransfer.Apply(local, UserDataTransfer.Export(remote, "x", When, false), ImportMode.Replace);

        var outfit = Assert.Single(local.Outfits);
        Assert.Equal("Remote", outfit.Name);
        Assert.Empty(local.Pins);
        Assert.Empty(local.DresserSnapshots);
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
        var file = UserDataTransfer.Export(SampleState(), "x", When, false);

        Assert.Equal(new SectionCounts(1, 2, 1, 2, 1), UserDataTransfer.Preview(file));
        Assert.Equal(7, UserDataTransfer.Preview(file).Total);
    }
}
