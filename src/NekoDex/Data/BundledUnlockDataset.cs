using System;
using System.IO;
using Dalamud.Plugin.Services;
using NekoDex.Core.Unlock;

namespace NekoDex.Data;

/// <summary>Loads the unlock dataset embedded in the plugin assembly (see tools/DataGen). Works offline; a broken file is logged and skipped.</summary>
internal static class BundledUnlockDataset
{
    private const string ResourceName = "NekoDex.Data.unlocks.json";

    public static DatasetUnlockSourceProvider? Load(IPluginLog log)
    {
        try
        {
            using var stream = typeof(BundledUnlockDataset).Assembly.GetManifestResourceStream(ResourceName);
            if (stream is null)
            {
                log.Error("Embedded resource {Name} is missing; in-game unlock info disabled", ResourceName);
                return null;
            }

            var dataset = UnlockDataset.Parse(stream);
            var provider = new DatasetUnlockSourceProvider(dataset);
            log.Information("Unlock dataset {Version} for game {Game} loaded ({Modules} modules)", dataset.DataVersion, dataset.GameVersion, dataset.Modules.Count);
            return provider;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            log.Error(ex, "Could not read the embedded unlock dataset; in-game unlock info disabled");
            return null;
        }
    }
}
