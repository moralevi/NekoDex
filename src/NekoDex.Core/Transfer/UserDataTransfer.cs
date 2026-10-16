using System.Text.Json;
using System.Text.Json.Serialization;
using NekoDex.Core.Checklist;

namespace NekoDex.Core.Transfer;

/// <summary>The export file. Every section is optional; unknown sections (from older exports) are ignored.</summary>
public sealed class UserDataFile
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string ExportedAt { get; set; } = string.Empty;

    public string PluginVersion { get; set; } = string.Empty;

    public Dictionary<string, List<uint>>? Pins { get; set; }

    public List<ChecklistItem>? Checklist { get; set; }

    public List<string>? EnabledAutoItems { get; set; }
}

/// <summary>Live references to the user's data, so applying an import mutates the configuration in place.</summary>
public sealed class UserDataState
{
    public required Dictionary<string, List<uint>> Pins { get; init; }

    public required List<ChecklistItem> Checklist { get; init; }

    public required List<string> EnabledAutoItems { get; init; }
}

public enum ImportMode
{
    /// <summary>Add what is missing; on an id clash the local copy wins.</summary>
    Merge = 0,

    /// <summary>Sections present in the file replace the local ones; absent sections are left alone.</summary>
    Replace = 1,
}

/// <summary>What an import contains or did, per section.</summary>
public sealed record SectionCounts(int Pins, int Checklist, int AutoItems)
{
    public int Total => Pins + Checklist + AutoItems;
}

public static class UserDataTransfer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static UserDataFile Export(UserDataState state, string pluginVersion, DateTimeOffset now) => new()
    {
        ExportedAt = now.ToUniversalTime().ToString("O"),
        PluginVersion = pluginVersion,
        Pins = state.Pins.ToDictionary(p => p.Key, p => new List<uint>(p.Value)),
        Checklist = new List<ChecklistItem>(state.Checklist),
        EnabledAutoItems = new List<string>(state.EnabledAutoItems),
    };

    public static string ToJson(UserDataFile file) => JsonSerializer.Serialize(file, Options);

    /// <summary>Throws <see cref="InvalidDataException"/> for a different schema version or a file that is not an export.</summary>
    public static UserDataFile Parse(string json)
    {
        UserDataFile? file;
        try
        {
            file = JsonSerializer.Deserialize<UserDataFile>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Not a NekoDex export file.", ex);
        }

        if (file is null)
        {
            throw new InvalidDataException("Not a NekoDex export file.");
        }

        if (file.SchemaVersion != UserDataFile.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Export schema v{file.SchemaVersion} is not supported by this version (expected v{UserDataFile.CurrentSchemaVersion}).");
        }

        return file;
    }

    public static SectionCounts Preview(UserDataFile file) => new(
        file.Pins?.Sum(p => p.Value.Count) ?? 0,
        file.Checklist?.Count ?? 0,
        file.EnabledAutoItems?.Count ?? 0);

    /// <summary>Applies the file to the live state and returns how many entries were added or replaced per section.</summary>
    public static SectionCounts Apply(UserDataState target, UserDataFile file, ImportMode mode)
    {
        var pins = 0;
        var checklist = 0;
        var auto = 0;

        if (file.Pins is { } filePins)
        {
            if (mode == ImportMode.Replace)
            {
                target.Pins.Clear();
            }

            foreach (var (module, ids) in filePins)
            {
                if (!target.Pins.TryGetValue(module, out var local))
                {
                    target.Pins[module] = local = new List<uint>();
                }

                foreach (var id in ids)
                {
                    if (!local.Contains(id))
                    {
                        local.Add(id);
                        pins++;
                    }
                }
            }
        }

        if (file.Checklist is { } fileChecklist)
        {
            if (mode == ImportMode.Replace)
            {
                target.Checklist.Clear();
            }

            foreach (var item in fileChecklist)
            {
                if (target.Checklist.Any(c => c.Id == item.Id))
                {
                    continue;
                }

                target.Checklist.Add(item);
                checklist++;
            }
        }

        if (file.EnabledAutoItems is { } fileAuto)
        {
            if (mode == ImportMode.Replace)
            {
                target.EnabledAutoItems.Clear();
            }

            foreach (var id in fileAuto)
            {
                if (!target.EnabledAutoItems.Contains(id))
                {
                    target.EnabledAutoItems.Add(id);
                    auto++;
                }
            }
        }

        return new SectionCounts(pins, checklist, auto);
    }
}
