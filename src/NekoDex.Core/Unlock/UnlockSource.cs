namespace NekoDex.Core.Unlock;

/// <summary>
/// How to obtain one entry. <paramref name="Summary"/> is one short line; <paramref name="Url"/> is an optional web page.
/// A bundled dataset provider (Phase 4) can extend this with steps and requirements without breaking link-only consumers.
/// </summary>
public sealed record UnlockSource(UnlockSourceType Type, string Summary, string? Url = null)
{
    public static UnlockSource Unknown(string? url = null) => new(UnlockSourceType.Unknown, string.Empty, url);

    public bool IsUnknown => Type == UnlockSourceType.Unknown;
}
