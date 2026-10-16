using System.Text.Json;
using System.Text.Json.Serialization;

namespace NekoDex.Core.Unlock;

/// <summary>
/// The bundled unlock data file. Produced by tools/DataGen, embedded in the plugin, parsed at load.
/// Keys are module ids ("mounts") and entry row ids.
/// </summary>
public sealed class UnlockDataset
{
    /// <summary>Bump when the JSON shape changes incompatibly.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Human-readable data version, the generation date (yyyy-MM-dd).</summary>
    public string DataVersion { get; set; } = string.Empty;

    /// <summary>Game version the generator ran against (contents of ffxivgame.ver).</summary>
    public string GameVersion { get; set; } = string.Empty;

    /// <summary>Where the unlock text came from; shown as attribution.</summary>
    public string Source { get; set; } = string.Empty;

    public Dictionary<string, UnlockDatasetModule> Modules { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static UnlockDataset Parse(Stream json)
    {
        var dataset = JsonSerializer.Deserialize<UnlockDataset>(json, Options) ?? throw new InvalidDataException("Unlock dataset is empty.");
        if (dataset.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unlock dataset schema v{dataset.SchemaVersion} is not supported (expected v{CurrentSchemaVersion}).");
        }

        return dataset;
    }

    public static UnlockDataset Parse(string json)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return Parse(stream);
    }

    public string ToJson() => JsonSerializer.Serialize(this, Options);
}

public sealed class UnlockDatasetModule
{
    /// <summary>Highest entry id that existed in the game data when the dataset was generated. Anything above it is newer than the data.</summary>
    public uint MaxEntryId { get; set; }

    public Dictionary<uint, UnlockDatasetEntry> Entries { get; set; } = new();
}

public sealed class UnlockDatasetEntry
{
    public string? Patch { get; set; }

    /// <summary>The item that grants the collectable (whistle, roll, card...), for item-page links and Garland lookups.</summary>
    public uint? ItemId { get; set; }

    /// <summary>Where this entry's text came from when it differs from the dataset's main source.</summary>
    public string? Source { get; set; }

    public List<UnlockDatasetSource> Sources { get; set; } = new();
}

public sealed class UnlockDatasetSource
{
    /// <summary>Free-text category as the source site uses it ("Trial", "Tribal", ...). Mapped by <see cref="UnlockCategories"/>.</summary>
    public string Category { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
