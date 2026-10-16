using System;
using System.IO;
using Dalamud.Plugin.Services;
using NekoDex.Core.Transfer;

namespace NekoDex;

/// <summary>Export and import of the user's own data (outfits, pins, checklist, auto item choices, dresser snapshots) as one JSON file.</summary>
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
        Outfits = config.Current.Outfits,
        Pins = config.Current.Pins,
        Checklist = config.Current.Checklist,
        EnabledAutoItems = config.Current.EnabledAutoItems,
        DresserSnapshots = config.Current.Dresser.Snapshots,
    };

    public string DefaultFileName(bool outfitsOnly) => $"nekodex-{(outfitsOnly ? "outfits" : "data")}-{DateTime.Now:yyyy-MM-dd}.json";

    /// <summary>Writes the file; returns a message for the status line.</summary>
    public string Export(string path, bool outfitsOnly)
    {
        try
        {
            var file = UserDataTransfer.Export(State, pluginVersion, DateTimeOffset.UtcNow, outfitsOnly);
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
            Strings.Get("Data_Imported", "Imported: {0} outfits, {1} pins, {2} checklist items, {3} auto items, {4} dresser snapshots"),
            counts.Outfits, counts.Pins, counts.Checklist, counts.AutoItems, counts.Snapshots);
    }
}
