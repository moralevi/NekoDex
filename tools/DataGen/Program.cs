using System.Net.Http.Headers;
using System.Text.Json;
using Lumina;
using Lumina.Excel.Sheets;
using NekoDex.Core.Unlock;

// Usage (from the repo root):
//   dotnet run --project tools/DataGen [-- --out <file>] [--game <path>] [--input <saved api json>]
// Defaults: --out src/NekoDex/Data/unlocks.json, --game from XIVLauncher's launcherConfigV3.json, API fetched live.

var options = Options.Parse(args);
var gamePath = options.GamePath ?? GameLocator.FromXivLauncher() ?? Cli.Fail("Game path not found. Pass --game <path to 'FINAL FANTASY XIV - A Realm Reborn'>.");
var sqpack = Path.Combine(gamePath, "game", "sqpack");
if (!Directory.Exists(sqpack))
{
    Cli.Die($"No sqpack folder under {gamePath}.");
}

var gameVersion = File.ReadAllText(Path.Combine(gamePath, "game", "ffxivgame.ver")).Trim();
Console.WriteLine($"Game {gameVersion} at {gamePath}");

var lumina = new GameData(sqpack, new LuminaOptions { PanicOnSheetChecksumMismatch = false });
var mountSheet = lumina.GetExcelSheet<Mount>() ?? throw new InvalidOperationException("Mount sheet missing");
var guideMounts = mountSheet.Where(m => m.Icon != 0 && m.Order >= 0 && !m.Singular.IsEmpty).ToDictionary(m => m.RowId);
Console.WriteLine($"Mount sheet: {guideMounts.Count} guide rows, max id {guideMounts.Keys.Max()}");

var apiJson = options.InputFile is not null
    ? await File.ReadAllTextAsync(options.InputFile)
    : await FetchAsync(options.ApiUrl);
var api = JsonSerializer.Deserialize<CollectResponse>(apiJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
          ?? Cli.Fail<CollectResponse>("API response was empty.");
Console.WriteLine($"FFXIV Collect: {api.Results.Count} mounts");

var module = new UnlockDatasetModule { MaxEntryId = guideMounts.Keys.Max() };
var warnings = 0;
var unknownCategories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

foreach (var mount in api.Results.OrderBy(m => m.Id))
{
    if (!guideMounts.ContainsKey(mount.Id))
    {
        Console.WriteLine($"  warn: API mount {mount.Id} '{mount.Name}' is not a Mount Guide row in this game version; skipped");
        warnings++;
        continue;
    }

    var entry = new UnlockDatasetEntry { Patch = mount.Patch };
    foreach (var source in mount.Sources)
    {
        if (string.IsNullOrWhiteSpace(source.Text))
        {
            continue;
        }

        if (!UnlockCategories.IsKnown(source.Type))
        {
            unknownCategories.Add(source.Type);
        }

        entry.Sources.Add(new UnlockDatasetSource { Category = source.Type, Text = source.Text.Trim() });
    }

    if (entry.Sources.Count > 0)
    {
        module.Entries[mount.Id] = entry;
    }
}

foreach (var id in guideMounts.Keys.Where(id => !module.Entries.ContainsKey(id)).Order())
{
    Console.WriteLine($"  warn: no unlock data for mount {id} '{guideMounts[id].Singular.ExtractText()}'");
    warnings++;
}

foreach (var category in unknownCategories)
{
    Console.WriteLine($"  warn: category '{category}' is not in UnlockCategories; it will display as Other");
    warnings++;
}

var dataset = new UnlockDataset
{
    DataVersion = DateTime.UtcNow.ToString("yyyy-MM-dd"),
    GameVersion = gameVersion,
    Source = "FFXIV Collect",
    Modules = { ["mounts"] = module },
};

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutFile))!);
await File.WriteAllTextAsync(options.OutFile, dataset.ToJson() + "\n");
Console.WriteLine($"Wrote {module.Entries.Count} mount entries to {options.OutFile} (data {dataset.DataVersion}, {warnings} warnings)");
return warnings == 0 ? 0 : 2;

static async Task<string> FetchAsync(string url)
{
    using var http = new HttpClient();
    http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NekoDex-DataGen", "0.1"));
    http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://github.com/moralevi/NekoDex)"));
    Console.WriteLine($"GET {url}");
    return await http.GetStringAsync(url);
}

static class Cli
{
    public static T Fail<T>(string message)
    {
        Console.Error.WriteLine("error: " + message);
        Environment.Exit(1);
        return default!;
    }

    public static string Fail(string message) => Fail<string>(message);

    public static void Die(string message) => Fail<object>(message);
}

sealed record Options(string OutFile, string? GamePath, string? InputFile, string ApiUrl)
{
    public static Options Parse(string[] args)
    {
        string outFile = Path.Combine("src", "NekoDex", "Data", "unlocks.json");
        string? gamePath = null;
        string? inputFile = null;
        var apiUrl = "https://ffxivcollect.com/api/mounts";

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : Cli.Fail($"{args[i]} needs a value");
            switch (args[i])
            {
                case "--out": outFile = Next(); break;
                case "--game": gamePath = Next(); break;
                case "--input": inputFile = Next(); break;
                case "--api": apiUrl = Next(); break;
                default: Cli.Die($"Unknown argument {args[i]}"); break;
            }
        }

        return new Options(outFile, gamePath, inputFile, apiUrl);
    }
}

static class GameLocator
{
    public static string? FromXivLauncher()
    {
        var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher", "launcherConfigV3.json");
        if (!File.Exists(config))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(config));
        return doc.RootElement.TryGetProperty("GamePath", out var path) ? path.GetString() : null;
    }
}

sealed class CollectResponse
{
    public List<CollectMount> Results { get; set; } = new();
}

sealed class CollectMount
{
    public uint Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Patch { get; set; }

    public List<CollectSource> Sources { get; set; } = new();
}

sealed class CollectSource
{
    public string Type { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
