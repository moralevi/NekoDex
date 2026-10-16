using System.Net.Http.Headers;
using System.Text.Json;
using Lumina;
using Lumina.Excel.Sheets;
using NekoDex.Core.Unlock;

// Usage (from the repo root):
//   dotnet run --project tools/DataGen [-- --out <file>] [--game <path>] [--input-dir <dir with saved <module>.json>]
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

// One row per module: the same rules the plugin's Data/*Module.cs classes use to pick the rows the in-game guides show.
var modules = new ModuleSpec[]
{
    new("mounts", "https://ffxivcollect.com/api/mounts",
        () => lumina.GetExcelSheet<Mount>()!.Where(m => m.Icon != 0 && m.Order >= 0 && !m.Singular.IsEmpty).ToDictionary(m => m.RowId, m => m.Singular.ExtractText())),
    new("minions", "https://ffxivcollect.com/api/minions",
        () => lumina.GetExcelSheet<Companion>()!.Where(c => c.Icon != 0 && c.Order > 0 && !c.Singular.IsEmpty).ToDictionary(c => c.RowId, c => c.Singular.ExtractText())),
    new("orchestrion", "https://ffxivcollect.com/api/orchestrions",
        () =>
        {
            var ui = lumina.GetExcelSheet<OrchestrionUiparam>()!;
            return lumina.GetExcelSheet<Orchestrion>()!
                .Where(o => !o.Name.IsEmpty && ui.GetRowOrDefault(o.RowId) is { Order: > 0 })
                .ToDictionary(o => o.RowId, o => o.Name.ExtractText());
        }),
    new("emotes", "https://ffxivcollect.com/api/emotes",
        () => lumina.GetExcelSheet<Emote>()!.Where(e => !e.Name.IsEmpty && e.Icon != 0 && e.UnlockLink != 0).ToDictionary(e => e.RowId, e => e.Name.ExtractText())),
    new("bardings", "https://ffxivcollect.com/api/bardings",
        () => lumina.GetExcelSheet<BuddyEquip>()!.Where(b => !b.Name.IsEmpty && b.Order > 0).ToDictionary(b => b.RowId, b => b.Name.ExtractText())),
    new("fashion", "https://ffxivcollect.com/api/fashions",
        () => lumina.GetExcelSheet<Ornament>()!.Where(o => o.Icon != 0 && o.Order >= 0 && !o.Singular.IsEmpty).ToDictionary(o => o.RowId, o => o.Singular.ExtractText())),
    new("facewear", "https://ffxivcollect.com/api/facewear",
        () => lumina.GetExcelSheet<GlassesStyle>()!.Where(g => g.RowId > 0 && !g.Name.IsEmpty && g.Icon != 0).ToDictionary(g => g.RowId, g => g.Name.ExtractText())),
    new("triad", "https://ffxivcollect.com/api/triad/cards",
        () => lumina.GetExcelSheet<TripleTriadCard>()!.Where(c => c.RowId > 0 && !c.Name.IsEmpty).ToDictionary(c => c.RowId, c => c.Name.ExtractText())),
};

var dataset = new UnlockDataset
{
    DataVersion = DateTime.UtcNow.ToString("yyyy-MM-dd"),
    GameVersion = gameVersion,
    Source = "FFXIV Collect",
};

var warnings = 0;
var unknownCategories = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NekoDex-DataGen", "0.1"));
http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://github.com/moralevi/NekoDex)"));

foreach (var spec in modules)
{
    var guideRows = spec.GuideRows();
    Console.WriteLine($"[{spec.Id}] sheet: {guideRows.Count} guide rows, max id {guideRows.Keys.Max()}");

    string apiJson;
    if (options.InputDir is not null)
    {
        apiJson = await File.ReadAllTextAsync(Path.Combine(options.InputDir, spec.Id + ".json"));
    }
    else
    {
        Console.WriteLine($"[{spec.Id}] GET {spec.ApiUrl}");
        apiJson = await http.GetStringAsync(spec.ApiUrl);
    }

    var api = JsonSerializer.Deserialize<CollectResponse>(apiJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
              ?? Cli.Fail<CollectResponse>($"[{spec.Id}] API response was empty.");
    Console.WriteLine($"[{spec.Id}] FFXIV Collect: {api.Results.Count} entries");

    var module = new UnlockDatasetModule { MaxEntryId = guideRows.Keys.Max() };
    foreach (var row in api.Results.OrderBy(r => r.Id))
    {
        if (!guideRows.ContainsKey(row.Id))
        {
            Console.WriteLine($"  warn: API {spec.Id} {row.Id} '{row.Name}' is not a guide row in this game version; skipped");
            warnings++;
            continue;
        }

        var entry = new UnlockDatasetEntry { Patch = row.Patch };
        foreach (var source in row.Sources)
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
            module.Entries[row.Id] = entry;
        }
    }

    var missing = guideRows.Keys.Where(id => !module.Entries.ContainsKey(id)).Order().ToList();
    foreach (var id in missing.Take(25))
    {
        Console.WriteLine($"  warn: no unlock data for {spec.Id} {id} '{guideRows[id]}'");
    }

    if (missing.Count > 25)
    {
        Console.WriteLine($"  warn: ... and {missing.Count - 25} more {spec.Id} entries without data");
    }

    warnings += missing.Count;
    dataset.Modules[spec.Id] = module;
    Console.WriteLine($"[{spec.Id}] {module.Entries.Count} entries with data, {missing.Count} without");
}

foreach (var category in unknownCategories)
{
    Console.WriteLine($"  warn: category '{category}' is not in UnlockCategories; it will display as Other");
    warnings++;
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutFile))!);
await File.WriteAllTextAsync(options.OutFile, dataset.ToJson().ReplaceLineEndings("\n") + "\n");
Console.WriteLine($"Wrote {options.OutFile} (data {dataset.DataVersion}, {dataset.Modules.Count} modules, {warnings} warnings)");
return warnings == 0 ? 0 : 2;

sealed record ModuleSpec(string Id, string ApiUrl, Func<Dictionary<uint, string>> GuideRows);

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

sealed record Options(string OutFile, string? GamePath, string? InputDir)
{
    public static Options Parse(string[] args)
    {
        string outFile = Path.Combine("src", "NekoDex", "Data", "unlocks.json");
        string? gamePath = null;
        string? inputDir = null;

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : Cli.Fail($"{args[i]} needs a value");
            switch (args[i])
            {
                case "--out": outFile = Next(); break;
                case "--game": gamePath = Next(); break;
                case "--input-dir": inputDir = Next(); break;
                default: Cli.Die($"Unknown argument {args[i]}"); break;
            }
        }

        return new Options(outFile, gamePath, inputDir);
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
    public List<CollectRow> Results { get; set; } = new();
}

sealed class CollectRow
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
