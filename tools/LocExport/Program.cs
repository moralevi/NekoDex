using NekoDex.Core.Localization;

// Usage (from the repo root): dotnet run --project tools/LocExport [-- --src src/NekoDex --out loc/en.json]
var src = Path.Combine("src", "NekoDex");
var outFile = Path.Combine("loc", "en.json");
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--src" when i + 1 < args.Length: src = args[++i]; break;
        case "--out" when i + 1 < args.Length: outFile = args[++i]; break;
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

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
File.WriteAllText(outFile, LocExtractor.ToCheapLocJson(extraction.Strings) + "\n");
Console.WriteLine($"Wrote {extraction.Strings.Count} strings from {files.Count} files to {outFile}");
return extraction.Conflicts.Count == 0 ? 0 : 2;
