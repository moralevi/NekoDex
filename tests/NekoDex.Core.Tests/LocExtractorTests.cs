using NekoDex.Core.Localization;

namespace NekoDex.Core.Tests;

public class LocExtractorTests
{
    [Fact]
    public void Extracts_keys_and_fallbacks_across_files_in_key_order()
    {
        var a = """ImGui.Text(Strings.Get("Zebra", "Last")); var x = Strings.Get( "Alpha" , "First \"quoted\"" );""";
        var b = """Strings.Get("Mid", "Line\nbreak")""";

        var result = LocExtractor.Extract([a, b]);

        Assert.Equal(["Alpha", "Mid", "Zebra"], result.Strings.Keys);
        Assert.Equal("First \"quoted\"", result.Strings["Alpha"]);
        Assert.Equal("Line\nbreak", result.Strings["Mid"]);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void Same_key_with_different_fallback_is_a_conflict_and_keeps_the_first()
    {
        var result = LocExtractor.Extract(["""Strings.Get("K", "one")""", """Strings.Get("K", "two")""", """Strings.Get("K", "one")"""]);

        Assert.Equal("one", result.Strings["K"]);
        var conflict = Assert.Single(result.Conflicts);
        Assert.StartsWith("K:", conflict);
    }

    [Fact]
    public void Ignores_calls_with_non_literal_arguments()
    {
        var result = LocExtractor.Extract(["""Strings.Get(key, "x"); Strings.Get("A", fallback); Strings.Get("B", "ok")"""]);

        Assert.Equal(["B"], result.Strings.Keys);
    }

    [Fact]
    public void Compare_reports_missing_and_extra_keys_sorted()
    {
        var diff = LocExtractor.Compare(["B", "A", "C"], ["C", "Z", "A"]);

        Assert.Equal(["B"], diff.Missing);
        Assert.Equal(["Z"], diff.Extra);
    }

    [Fact]
    public void Writes_cheaploc_shape()
    {
        var json = LocExtractor.ToCheapLocJson(new Dictionary<string, string> { ["Hello"] = "Hi there" });

        Assert.Contains("\"Hello\": {", json);
        Assert.Contains("\"message\": \"Hi there\"", json);
        Assert.Contains("\"description\": \"\"", json);
    }
}
