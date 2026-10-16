using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NekoDex.Core.Localization;

/// <summary>
/// Finds every <c>Strings.Get("Key", "Fallback")</c> call in source text and turns them into a CheapLoc JSON file.
/// Pure, so the export tool stays a thin shell and this is tested.
/// </summary>
public static partial class LocExtractor
{
    [GeneratedRegex("""Strings\.Get\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)""", RegexOptions.Compiled)]
    private static partial Regex CallPattern();

    public sealed record Extraction(SortedDictionary<string, string> Strings, IReadOnlyList<string> Conflicts);

    /// <summary>Keys mapped to their English fallback. A key used with two different fallbacks is reported as a conflict and keeps the first.</summary>
    public static Extraction Extract(IEnumerable<string> sources)
    {
        var strings = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var conflicts = new List<string>();

        foreach (var source in sources)
        {
            foreach (Match match in CallPattern().Matches(source))
            {
                var key = Unescape(match.Groups[1].Value);
                var fallback = Unescape(match.Groups[2].Value);
                if (strings.TryGetValue(key, out var existing))
                {
                    if (existing != fallback)
                    {
                        conflicts.Add($"{key}: \"{existing}\" vs \"{fallback}\"");
                    }

                    continue;
                }

                strings[key] = fallback;
            }
        }

        return new Extraction(strings, conflicts);
    }

    /// <summary>CheapLoc's file shape: key to {message, description}.</summary>
    public static string ToCheapLocJson(IReadOnlyDictionary<string, string> strings)
    {
        var entries = new SortedDictionary<string, LocEntry>(StringComparer.Ordinal);
        foreach (var (key, message) in strings)
        {
            entries[key] = new LocEntry(message, string.Empty);
        }

        return JsonSerializer.Serialize(entries, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private sealed record LocEntry(string message, string description);

    private static string Unescape(string literal)
    {
        if (!literal.Contains('\\'))
        {
            return literal;
        }

        var sb = new StringBuilder(literal.Length);
        for (var i = 0; i < literal.Length; i++)
        {
            var c = literal[i];
            if (c != '\\' || i + 1 >= literal.Length)
            {
                sb.Append(c);
                continue;
            }

            var next = literal[++i];
            sb.Append(next switch
            {
                'n' => '\n',
                't' => '\t',
                '"' => '"',
                '\\' => '\\',
                _ => next,
            });
        }

        return sb.ToString();
    }
}
