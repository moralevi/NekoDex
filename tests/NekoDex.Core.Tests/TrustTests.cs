using NekoDex.Core.Collections;
using NekoDex.Core.Trust;
using NekoDex.Core.Unlock;

namespace NekoDex.Core.Tests;

public class TrustTests
{
    [Fact]
    public void Report_text_names_entry_module_sources_and_versions()
    {
        var entry = new CollectionEntry(18, "Behemoth", 0);
        var source = new UnlockSource(UnlockSourceType.Vendor, "Jonathas", Details: ["Achievement: Jonathas - Old Gridania"], Patch: "2.1", Note: "FFXIV Collect, data 2026-10-16");

        var text = ReportText.Build("mounts", entry, source, "2026-10-16", "1.0.0.0");

        Assert.Contains("Behemoth (mounts id 18)", text);
        Assert.Contains("FFXIV Collect, data 2026-10-16", text);
        Assert.Contains("  - Achievement: Jonathas - Old Gridania", text);
        Assert.Contains("Patch: 2.1", text);
        Assert.Contains("Correction: ", text);
        Assert.EndsWith("Dataset 2026-10-16, plugin 1.0.0.0", text);
    }

    [Fact]
    public void Report_text_for_unknown_says_so()
    {
        var text = ReportText.Build("minions", new CollectionEntry(1, "x", 0), UnlockSource.Unknown(), null, "1.0.0.0");

        Assert.Contains("unknown / no source", text);
        Assert.Contains("Dataset none", text);
    }

    [Theory]
    [InlineData("1.0.0.0", "2026-10-16", "1.0.0.0", "2026-10-16", UpdateStatus.UpToDate)]
    [InlineData("1.0.0.0", "2026-10-16", "1.1.0.0", "2026-10-16", UpdateStatus.NewerPluginAvailable)]
    [InlineData("1.0.0.0", "2026-10-16", "1.0.0.0", "2026-10-23", UpdateStatus.NewerDataAvailable)]
    [InlineData("1.1.0.0", "2026-10-23", "1.0.0.0", "2026-10-16", UpdateStatus.UpToDate)]
    [InlineData("1.0.0.0", "2026-10-16", "1.1.0.0", "2026-10-23", UpdateStatus.NewerPluginAvailable)]
    [InlineData("1.0.0.0", "2026-10-16", null, null, UpdateStatus.Unknown)]
    [InlineData("1.0.0.0", "2026-10-16", null, "2026-10-23\n", UpdateStatus.NewerDataAvailable)]
    public void Compares_plugin_then_data(string localPlugin, string localData, string? remotePlugin, string? remoteData, UpdateStatus expected)
    {
        Assert.Equal(expected, UpdateCheck.Compare(localPlugin, localData, remotePlugin, remoteData).Status);
    }

    [Fact]
    public void Reads_assembly_version_from_pluginmaster_json()
    {
        var json = """[ { "InternalName": "Other", "AssemblyVersion": "9.9.9.9" }, { "InternalName": "NekoDex", "AssemblyVersion": "1.2.0.0" } ]""";

        Assert.Equal("1.2.0.0", UpdateCheck.AssemblyVersionFromPluginMaster(json));
        Assert.Null(UpdateCheck.AssemblyVersionFromPluginMaster("not json"));
        Assert.Null(UpdateCheck.AssemblyVersionFromPluginMaster("[]"));
    }
}
