using System.Text.Json;
using NekoDex.Core.Localization;

// Usage (from the repo root):
//   dotnet run --project tools/LocExport                      regenerates loc/en.json from the plugin source
//   dotnet run --project tools/LocExport -- --check loc/sv.json   lists keys a translation is missing or has extra
// Options: --src <dir> (default src/NekoDex), --out <file> (default loc/en.json).
var src = Path.Combine("src", "NekoDex");
var outFile = Path.Combine("loc", "en.json");
string? check = null;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--src" when i + 1 < args.Length: src = args[++i]; break;
        case "--out" when i + 1 < args.Length: outFile = args[++i]; break;
        case "--check" when i + 1 < args.Length: check = args[++i]; break;
        default:
            Console.Error.WriteLine($"Unknown argument {args[i]}");
            return 1;
    }
}

var files = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
    .ToList();

var extraction = LocExtractor.Extract(files.Select(File.ReadAllText));
foreach (var conflict in extraction.Conflicts)
{
    Console.WriteLine("conflict: " + conflict);
}

if (check is not null)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(check));
    var translated = doc.RootElement.EnumerateObject().Select(p => p.Name).ToList();
    var diff = LocExtractor.Compare(extraction.Strings.Keys, translated);
    foreach (var key in diff.Missing)
    {
        Console.WriteLine($"missing: {key}  (en: \"{extraction.Strings[key]}\")");
    }

    foreach (var key in diff.Extra)
    {
        Console.WriteLine($"extra:   {key}");
    }

    Console.WriteLine($"{check}: {translated.Count - diff.Extra.Count} of {extraction.Strings.Count} keys translated, {diff.Missing.Count} missing, {diff.Extra.Count} extra");
    return diff.Missing.Count == 0 ? 0 : 2;
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
File.WriteAllText(outFile, LocExtractor.ToCheapLocJson(extraction.Strings).ReplaceLineEndings("\n") + "\n");
Console.WriteLine($"Wrote {extraction.Strings.Count} strings from {files.Count} files to {outFile}");
return extraction.Conflicts.Count == 0 ? 0 : 2;
