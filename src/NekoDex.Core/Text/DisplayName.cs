using System.Text;

namespace NekoDex.Core.Text;

/// <summary>Turns the game's lower-case English sheet names ("company chocobo") into display names ("Company Chocobo").</summary>
public static class DisplayName
{
    private static readonly HashSet<string> SmallWords = new(StringComparer.Ordinal)
    {
        "a", "an", "and", "at", "by", "for", "in", "of", "on", "or", "the", "to", "with",
    };

    /// <summary>
    /// Capitalises the first letter of each word, except small words after the first.
    /// Hyphenated parts are capitalised individually ("wind-up" → "Wind-Up"). Already-capitalised letters are kept.
    /// </summary>
    public static string TitleCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var sb = new StringBuilder(name.Length);
        var wordStart = 0;
        var first = true;

        for (var i = 0; i <= name.Length; i++)
        {
            if (i < name.Length && name[i] != ' ')
            {
                continue;
            }

            if (i > wordStart)
            {
                var word = name.AsSpan(wordStart, i - wordStart);
                AppendWord(sb, word, first);
                first = false;
            }

            if (i < name.Length)
            {
                sb.Append(' ');
            }

            wordStart = i + 1;
        }

        return sb.ToString();
    }

    private static void AppendWord(StringBuilder sb, ReadOnlySpan<char> word, bool first)
    {
        if (!first && SmallWords.Contains(word.ToString()))
        {
            sb.Append(word);
            return;
        }

        var capitalizeNext = true;
        foreach (var c in word)
        {
            sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
            capitalizeNext = c == '-';
        }
    }
}
