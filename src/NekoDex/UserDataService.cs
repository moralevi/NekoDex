using System;
using System.IO;
using Dalamud.Plugin.Services;
using NekoDex.Core.Transfer;

namespace NekoDex;

/// <summary>Export and import of the user's own data (pins, checklist, auto item choices) as one JSON file.</summary>
public sealed class UserDataService
{
    private readonly ConfigurationManager config;
    private readonly IPluginLog log;
    private readonly string pluginVersion;

    public UserDataService(ConfigurationManager config, IPluginLog log, string pluginVersion)
    {
        this.config = config;
        this.log = log;
        this.pluginVersion = pluginVersion;
    }

    public UserDataState State => new()
    {
        Pins = config.Current.Pins,
        Checklist = config.Current.Checklist,
        EnabledAutoItems = config.Current.EnabledAutoItems,
    };

    public string DefaultFileName() => $"nekodex-data-{DateTime.Now:yyyy-MM-dd}.json";

    /// <summary>Writes the file; returns a message for the status line.</summary>
    public string Export(string path)
    {
        try
        {
            var file = UserDataTransfer.Export(State, pluginVersion, DateTimeOffset.UtcNow);
            File.WriteAllText(path, UserDataTransfer.ToJson(file) + "\n");
            var counts = UserDataTransfer.Preview(file);
            log.Information("Exported user data to {Path}: {Counts}", path, counts);
            return string.Format(Strings.Get("Data_Exported", "Exported {0} entries to {1}"), counts.Total, Path.GetFileName(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Error(ex, "Export failed");
            return string.Format(Strings.Get("Data_ExportFailed", "Export failed: {0}"), ex.Message);
        }
    }

    /// <summary>Parses a file for preview. Returns null and an error message when it cannot be read.</summary>
    public (UserDataFile? File, string? Error) Load(string path)
    {
        try
        {
            return (UserDataTransfer.Parse(File.ReadAllText(path)), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            log.Warning(ex, "Import of {Path} refused", path);
            return (null, ex.Message);
        }
    }

    public string Import(UserDataFile file, ImportMode mode)
    {
        var counts = UserDataTransfer.Apply(State, file, mode);
        config.Save();
        log.Information("Imported user data ({Mode}): {Counts}", mode, counts);
        return string.Format(
            Strings.Get("Data_Imported", "Imported: {0} pins, {1} checklist items, {2} auto items"),
            counts.Pins, counts.Checklist, counts.AutoItems);
    }
}
