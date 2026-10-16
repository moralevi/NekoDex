namespace NekoDex.Core.Unlock;

/// <summary>
/// How to obtain one entry.
/// </summary>
/// <param name="Type">Coarse category used for labels and filtering.</param>
/// <param name="Summary">One short line for tooltips and compact views.</param>
/// <param name="Url">Optional web page with more detail.</param>
/// <param name="Details">Every known way to obtain the entry, one line each, for the detail pane.</param>
/// <param name="Patch">Game patch the entry was added in, when known.</param>
/// <param name="Note">Attribution or staleness note, e.g. the dataset the answer came from.</param>
/// <param name="Categories">Raw source categories ("Trial", "Premium", ...), one per way to obtain; drives the obtainable filter.</param>
/// <param name="ItemId">The item that grants the entry, when known; lets link sites that index by item (Garland Tools) be used.</param>
public sealed record UnlockSource(
    UnlockSourceType Type,
    string Summary,
    string? Url = null,
    IReadOnlyList<string>? Details = null,
    string? Patch = null,
    string? Note = null,
    IReadOnlyList<string>? Categories = null,
    uint? ItemId = null)
{
    public static UnlockSource Unknown(string? url = null, string? note = null) => new(UnlockSourceType.Unknown, string.Empty, url, Note: note);

    public bool IsUnknown => Type == UnlockSourceType.Unknown;

    /// <summary>Detail lines, falling back to the summary so the detail pane never shows nothing for a known source.</summary>
    public IReadOnlyList<string> DetailLines => Details is { Count: > 0 } ? Details : string.IsNullOrEmpty(Summary) ? [] : [Summary];
}
