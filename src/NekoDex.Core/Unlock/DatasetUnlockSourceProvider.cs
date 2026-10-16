using NekoDex.Core.Collections;

namespace NekoDex.Core.Unlock;

/// <summary>
/// Answers from the bundled <see cref="UnlockDataset"/>. Works offline.
/// Entries newer than the dataset come back as an explicit Unknown with a staleness note (the resolver then adds the web link);
/// entries the dataset simply lacks come back as null so a later provider may answer.
/// </summary>
public sealed class DatasetUnlockSourceProvider : IUnlockSourceProvider
{
    private readonly UnlockDataset dataset;
    private readonly Dictionary<string, string?> latestPatch = new();

    public DatasetUnlockSourceProvider(UnlockDataset dataset)
    {
        this.dataset = dataset;
    }

    /// <summary>The newest patch any entry of the module was added in, or null when the module has no patch data.</summary>
    public string? LatestPatch(string moduleId)
    {
        if (latestPatch.TryGetValue(moduleId, out var cached))
        {
            return cached;
        }

        string? latest = null;
        if (dataset.Modules.TryGetValue(moduleId, out var module))
        {
            foreach (var entry in module.Entries.Values)
            {
                if (entry.Patch is not null && (latest is null || Collections.EntryViewQuery.ComparePatch(entry.Patch, latest) > 0))
                {
                    latest = entry.Patch;
                }
            }
        }

        latestPatch[moduleId] = latest;
        return latest;
    }

    public string DataVersion => dataset.DataVersion;

    public string GameVersion => dataset.GameVersion;

    public string Source => dataset.Source;

    /// <summary>Shown next to every answer, e.g. "FFXIV Collect, data 2026-10-16".</summary>
    public string Attribution => $"{dataset.Source}, data {dataset.DataVersion}";

    public bool IsNewerThanData(string moduleId, CollectionEntry entry)
        => dataset.Modules.TryGetValue(moduleId, out var module) && entry.Id > module.MaxEntryId;

    public UnlockSource? GetSource(string moduleId, CollectionEntry entry)
    {
        if (!dataset.Modules.TryGetValue(moduleId, out var module))
        {
            return null;
        }

        if (entry.Id > module.MaxEntryId)
        {
            return UnlockSource.Unknown(note: $"Newer than the bundled data ({dataset.DataVersion}).");
        }

        if (!module.Entries.TryGetValue(entry.Id, out var data) || data.Sources.Count == 0)
        {
            return null;
        }

        var lines = new string[data.Sources.Count];
        var categories = new string[data.Sources.Count];
        for (var i = 0; i < lines.Length; i++)
        {
            categories[i] = data.Sources[i].Category;
            lines[i] = $"{data.Sources[i].Category}: {data.Sources[i].Text}";
        }

        var first = data.Sources[0];
        return new UnlockSource(
            UnlockCategories.ToType(first.Category),
            lines.Length == 1 ? first.Text : $"{first.Text} (+{lines.Length - 1})",
            Details: lines,
            Patch: data.Patch,
            Note: Attribution,
            Categories: categories);
    }
}
