using CheapLoc;

namespace NekoDex;

/// <summary>
/// All user-visible text goes through here so it can be localised later.
/// Keys are stable identifiers; the fallback is the English text.
/// </summary>
internal static class Strings
{
    public static void Setup() => Loc.SetupWithFallbacks(typeof(Strings).Assembly);

    public static string Get(string key, string fallback) => Loc.Localize(key, fallback);
}
