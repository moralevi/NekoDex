using System.Net.Http.Headers;
using System.Text.Json;
using Lumina;
using Lumina.Excel.Sheets;
using NekoDex.Core.Unlock;

// Usage (from the repo root):
//   dotnet run --project tools/DataGen [-- --out <file>] [--game <path>] [--input-dir <dir with saved <module>.json>] [--no-game] [--no-garland]
// Defaults: --out src/NekoDex/Data/unlocks.json, --game from XIVLauncher's launcherConfigV3.json, APIs fetched live.
// Sources: FFXIV Collect first; entries it leaves without sources are looked up in Garland Tools item documents by item id.

var options = Options.Parse(args);
GameData? lumina = null;
var gameVersion = "unvalidated (generated without game files)";
if (options.NoGame)
{
    Console.WriteLine("Running without game files: sheet validation skipped, max entry id taken from the API.");
}
else
{
    var gamePath = options.GamePath ?? GameLocator.FromXivLauncher() ?? Cli.Fail("Game path not found. Pass --game <path to 'FINAL FANTASY XIV - A Realm Reborn'> or --no-game.");
    var sqpack = Path.Combine(gamePath, "game", "sqpack");
    if (!Directory.Exists(sqpack))
    {
        Cli.Die($"No sqpack folder under {gamePath}.");
    }

    gameVersion = File.ReadAllText(Path.Combine(gamePath, "game", "ffxivgame.ver")).Trim();
    Console.WriteLine($"Game {gameVersion} at {gamePath}");
    lumina = new GameData(sqpack, new LuminaOptions { PanicOnSheetChecksumMismatch = false });
}

GameData Sheets() => lumina ?? throw new InvalidOperationException("Game data not loaded");

// One row per module: the same rules the plugin's Data/*Module.cs classes use to pick the rows the in-game guides show.
var modules = new ModuleSpec[]
{
    new("mounts", "https://ffxivcollect.com/api/mounts",
        () => Sheets().GetExcelSheet<Mount>()!.Where(m => m.Icon != 0 && m.Order >= 0 && !m.Singular.IsEmpty).ToDictionary(m => m.RowId, m => m.Singular.ExtractText())),
    new("minions", "https://ffxivcollect.com/api/minions",
        () => Sheets().GetExcelSheet<Companion>()!.Where(c => c.Icon != 0 && c.Order > 0 && !c.Singular.IsEmpty).ToDictionary(c => c.RowId, c => c.Singular.ExtractText())),
    new("orchestrion", "https://ffxivcollect.com/api/orchestrions",
        () =>
        {
            var ui = Sheets().GetExcelSheet<OrchestrionUiparam>()!;
            return Sheets().GetExcelSheet<Orchestrion>()!
                .Where(o => !o.Name.IsEmpty && ui.GetRowOrDefault(o.RowId) is { Order: > 0 })
                .ToDictionary(o => o.RowId, o => o.Name.ExtractText());
        }),
    new("emotes", "https://ffxivcollect.com/api/emotes",
        () => Sheets().GetExcelSheet<Emote>()!.Where(e => !e.Name.IsEmpty && e.Icon != 0 && e.UnlockLink != 0).ToDictionary(e => e.RowId, e => e.Name.ExtractText())),
    new("bardings", "https://ffxivcollect.com/api/bardings",
        () => Sheets().GetExcelSheet<BuddyEquip>()!.Where(b => !b.Name.IsEmpty && b.Order > 0).ToDictionary(b => b.RowId, b => b.Name.ExtractText())),
    new("fashion", "https://ffxivcollect.com/api/fashions",
        () => Sheets().GetExcelSheet<Ornament>()!.Where(o => o.Icon != 0 && o.Order >= 0 && !o.Singular.IsEmpty).ToDictionary(o => o.RowId, o => o.Singular.ExtractText())),
    new("facewear", "https://ffxivcollect.com/api/facewear",
        () => Sheets().GetExcelSheet<GlassesStyle>()!.Where(g => g.RowId > 0 && !g.Name.IsEmpty && g.Icon != 0).ToDictionary(g => g.RowId, g => g.Name.ExtractText())),
    new("triad", "https://ffxivcollect.com/api/triad/cards",
        () => Sheets().GetExcelSheet<TripleTriadCard>()!.Where(c => c.RowId > 0 && !c.Name.IsEmpty).ToDictionary(c => c.RowId, c => c.Name.ExtractText())),
};

// Collectable -> item that grants it, from the Item sheet's ItemAction. Used for Garland lookups and item-page links.
var itemLinks = options.NoGame ? new Dictionary<string, Dictionary<uint, uint>>() : ItemLinker.Build(Sheets());

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
var garland = new GarlandClient(http);

foreach (var spec in modules)
{
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

    // Without game files every API row counts as a guide row; the next local run re-validates against the sheets.
    var guideRows = options.NoGame
        ? api.Results.ToDictionary(r => r.Id, r => r.Name)
        : spec.GuideRows();
    Console.WriteLine($"[{spec.Id}] {(options.NoGame ? "api" : "sheet")}: {guideRows.Count} guide rows, max id {guideRows.Keys.Max()}");

    itemLinks.TryGetValue(spec.Id, out var itemsForModule);
    var module = new UnlockDatasetModule { MaxEntryId = guideRows.Keys.Max() };
    var collectItemIds = new Dictionary<uint, uint>();

    foreach (var row in api.Results.OrderBy(r => r.Id))
    {
        if (!guideRows.ContainsKey(row.Id))
        {
            Console.WriteLine($"  warn: API {spec.Id} {row.Id} '{row.Name}' is not a guide row in this game version; skipped");
            warnings++;
            continue;
        }

        if (row.ItemId is { } collectItem && collectItem != 0)
        {
            collectItemIds[row.Id] = collectItem;
        }

        var entry = new UnlockDatasetEntry { Patch = row.Patch, Source = "FFXIV Collect" };
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

    // Item ids: the game sheet first, FFXIV Collect's item_id otherwise.
    uint? ItemIdFor(uint entryId)
    {
        if (itemsForModule is not null && itemsForModule.TryGetValue(entryId, out var fromSheet))
        {
            return fromSheet;
        }

        return collectItemIds.TryGetValue(entryId, out var fromCollect) ? fromCollect : null;
    }

    foreach (var (entryId, entry) in module.Entries)
    {
        entry.ItemId = ItemIdFor(entryId);
    }

    // Garland Tools fills the gaps FFXIV Collect left, when the entry has an item.
    var missingBefore = guideRows.Keys.Where(id => !module.Entries.ContainsKey(id)).Order().ToList();
    var filledByGarland = 0;
    if (!options.NoGarland)
    {
        foreach (var id in missingBefore)
        {
            if (ItemIdFor(id) is not { } itemId)
            {
                continue;
            }

            var sources = await garland.SourcesForItem(itemId);
            if (sources.Count == 0)
            {
                continue;
            }

            module.Entries[id] = new UnlockDatasetEntry { Source = "Garland Tools", ItemId = itemId, Sources = sources };
            filledByGarland++;
        }
    }

    var missing = guideRows.Keys.Where(id => !module.Entries.ContainsKey(id)).Order().ToList();
    foreach (var id in missing.Take(25))
    {
        Console.WriteLine($"  warn: no unlock data for {spec.Id} {id} '{guideRows[id]}'{(ItemIdFor(id) is null ? " (no item id either)" : string.Empty)}");
    }

    if (missing.Count > 25)
    {
        Console.WriteLine($"  warn: ... and {missing.Count - 25} more {spec.Id} entries without data");
    }

    warnings += missing.Count;
    dataset.Modules[spec.Id] = module;
    Console.WriteLine($"[{spec.Id}] {module.Entries.Count} entries with data ({filledByGarland} from Garland Tools), unknown {missingBefore.Count} -> {missing.Count}, item ids for {module.Entries.Values.Count(e => e.ItemId is not null)}");
}

foreach (var category in unknownCategories)
{
    Console.WriteLine($"  warn: category '{category}' is not in UnlockCategories; it will display as Other");
    warnings++;
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutFile))!);
await File.WriteAllTextAsync(options.OutFile, dataset.ToJson().ReplaceLineEndings("\n") + "\n");
Console.WriteLine($"Wrote {options.OutFile} (data {dataset.DataVersion}, {dataset.Modules.Count} modules, {warnings} warnings, {garland.Requests} Garland requests)");
return warnings == 0 ? 0 : 2;

sealed record ModuleSpec(string Id, string ApiUrl, Func<Dictionary<uint, string>> GuideRows);

/// <summary>Maps collectable ids to the item that unlocks them, by ItemAction type (verified 2026-10-16).</summary>
static class ItemLinker
{
    public static Dictionary<string, Dictionary<uint, uint>> Build(GameData gd)
    {
        var result = new Dictionary<string, Dictionary<uint, uint>>();
        void Add(string module, uint entryId, uint itemId)
        {
            if (!result.TryGetValue(module, out var map))
            {
                result[module] = map = new Dictionary<uint, uint>();
            }

            map.TryAdd(entryId, itemId);
        }

        var glasses = gd.GetExcelSheet<Glasses>()!;
        foreach (var item in gd.GetExcelSheet<Item>()!)
        {
            if (item.ItemAction.RowId == 0 || !item.ItemAction.IsValid)
            {
                continue;
            }

            var action = item.ItemAction.Value;
            var data0 = action.Data[0];
            switch (action.Action.RowId)
            {
                case 1322: Add("mounts", data0, item.RowId); break;
                case 853: Add("minions", data0, item.RowId); break;
                case 1013: Add("bardings", data0, item.RowId); break;
                case 20086: Add("fashion", data0, item.RowId); break;
                case 3357: Add("triad", data0, item.RowId); break;
                case 2633: Add("emotes", data0, item.RowId); break;
                case 25183:
                    if (item.AdditionalData.RowId != 0)
                    {
                        Add("orchestrion", item.AdditionalData.RowId, item.RowId);
                    }

                    break;
                case 37312:
                    if (item.AdditionalData.RowId != 0 && glasses.TryGetRow(item.AdditionalData.RowId, out var g) && g.Style.RowId != 0)
                    {
                        Add("facewear", g.Style.RowId, item.RowId);
                    }

                    break;
            }
        }

        Console.WriteLine("Item links: " + string.Join(", ", result.Select(kv => $"{kv.Key} {kv.Value.Count}")));
        return result;
    }
}

/// <summary>Reads Garland Tools item documents and turns their structured source fields into dataset sources.</summary>
sealed class GarlandClient(HttpClient http)
{
    public int Requests { get; private set; }

    public async Task<List<UnlockDatasetSource>> SourcesForItem(uint itemId)
    {
        Requests++;
        await Task.Delay(150);
        using var response = await http.GetAsync($"https://www.garlandtools.org/db/doc/item/en/3/{itemId}.json");
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        if (!root.TryGetProperty("item", out var item))
        {
            return [];
        }

        var names = new Dictionary<(string Type, string Id), string>();
        if (root.TryGetProperty("partials", out var partials))
        {
            foreach (var partial in partials.EnumerateArray())
            {
                var type = partial.GetProperty("type").GetString() ?? string.Empty;
                var id = partial.GetProperty("id").ToString();
                if (partial.TryGetProperty("obj", out var obj) && obj.TryGetProperty("n", out var n))
                {
                    names[(type, id)] = n.GetString() ?? id;
                }
            }
        }

        string Name(string type, JsonElement id) => names.TryGetValue((type, id.ToString()), out var name) ? name : $"{type} {id}";

        var sources = new List<UnlockDatasetSource>();
        void AddIds(string property, string type, string category)
        {
            if (!item.TryGetProperty(property, out var ids))
            {
                return;
            }

            foreach (var id in ids.EnumerateArray())
            {
                sources.Add(new UnlockDatasetSource { Category = category, Text = Name(type, id) });
            }
        }

        AddIds("quests", "quest", "Quest");
        AddIds("achievements", "achievement", "Achievement");
        AddIds("instances", "instance", "Duty");
        AddIds("fates", "fate", "FATE");
        AddIds("ventures", "venture", "Venture");

        if (item.TryGetProperty("tradeShops", out var shops))
        {
            foreach (var shop in shops.EnumerateArray())
            {
                var shopName = shop.TryGetProperty("shop", out var sn) ? sn.GetString() : null;
                var npc = shop.TryGetProperty("npcs", out var npcs) && npcs.GetArrayLength() > 0 ? Name("npc", npcs[0]) : null;
                var cost = string.Empty;
                if (shop.TryGetProperty("listings", out var listings) && listings.GetArrayLength() > 0 && listings[0].TryGetProperty("currency", out var currency))
                {
                    cost = string.Join(" + ", currency.EnumerateArray().Select(c => $"{c.GetProperty("amount")} {Name("item", c.GetProperty("id"))}"));
                }

                var parts = new[] { npc, shopName, cost }.Where(p => !string.IsNullOrEmpty(p));
                sources.Add(new UnlockDatasetSource { Category = "Trade", Text = string.Join(" - ", parts) });
            }
        }

        if (item.TryGetProperty("vendors", out var vendors))
        {
            var price = item.TryGetProperty("price", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt64() : 0;
            foreach (var vendor in vendors.EnumerateArray().Take(3))
            {
                sources.Add(new UnlockDatasetSource { Category = "Purchase", Text = price > 0 ? $"{Name("npc", vendor)} - {price:N0} gil" : Name("npc", vendor) });
            }
        }

        if (item.TryGetProperty("craft", out var craft) && craft.GetArrayLength() > 0)
        {
            sources.Add(new UnlockDatasetSource { Category = "Crafting", Text = "Crafted" });
        }

        if (item.TryGetProperty("drops", out var drops))
        {
            foreach (var mob in drops.EnumerateArray().Take(3))
            {
                sources.Add(new UnlockDatasetSource { Category = "Other", Text = $"Dropped by {Name("mob", mob)}" });
            }
        }

        return sources;
    }
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

sealed record Options(string OutFile, string? GamePath, string? InputDir, bool NoGame, bool NoGarland)
{
    public static Options Parse(string[] args)
    {
        string outFile = Path.Combine("src", "NekoDex", "Data", "unlocks.json");
        string? gamePath = null;
        string? inputDir = null;
        var noGame = false;
        var noGarland = false;

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : Cli.Fail($"{args[i]} needs a value");
            switch (args[i])
            {
                case "--out": outFile = Next(); break;
                case "--game": gamePath = Next(); break;
                case "--input-dir": inputDir = Next(); break;
                case "--no-game": noGame = true; break;
                case "--no-garland": noGarland = true; break;
                default: Cli.Die($"Unknown argument {args[i]}"); break;
            }
        }

        return new Options(outFile, gamePath, inputDir, noGame, noGarland);
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

    [System.Text.Json.Serialization.JsonPropertyName("item_id")]
    public uint? ItemId { get; set; }

    public List<CollectSource> Sources { get; set; } = new();
}

sealed class CollectSource
{
    public string Type { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}
