using NekoDex.Core.Unlock;

namespace NekoDex.UI;

internal static class UnlockLabels
{
    public static string For(UnlockSourceType type) => type switch
    {
        UnlockSourceType.Quest => Strings.Get("Unlock_Quest", "Quest"),
        UnlockSourceType.Duty => Strings.Get("Unlock_Duty", "Duty"),
        UnlockSourceType.Vendor => Strings.Get("Unlock_Vendor", "Vendor"),
        UnlockSourceType.Achievement => Strings.Get("Unlock_Achievement", "Achievement"),
        UnlockSourceType.Event => Strings.Get("Unlock_Event", "Event"),
        UnlockSourceType.Store => Strings.Get("Unlock_Store", "Store"),
        UnlockSourceType.Other => Strings.Get("Unlock_Other", "Other"),
        UnlockSourceType.Legacy => Strings.Get("Unlock_Legacy", "Legacy"),
        _ => Strings.Get("Unlock_Unknown", "Unknown"),
    };
}
