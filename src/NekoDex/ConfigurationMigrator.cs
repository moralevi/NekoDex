using System;
using Dalamud.Plugin.Services;
using NekoDex.Core.Dresser;

namespace NekoDex;

internal static class ConfigurationMigrator
{
    /// <summary>Upgrades <paramref name="config"/> in place. Returns true when something changed and a save is needed.</summary>
    public static bool Migrate(Configuration config, IPluginLog log)
    {
        if (config.Version >= Configuration.CurrentVersion)
        {
            return false;
        }

        var from = config.Version;
        while (config.Version < Configuration.CurrentVersion)
        {
            switch (config.Version)
            {
                case 0:
                    // v0 -> v1: first versioned release. Nothing to transform.
                    config.Version = 1;
                    break;

                case 1:
                    // v1 -> v2: the single Glamour Dresser snapshot becomes per character. We do not know whose it was,
                    // so it waits in the legacy slot until a character logs in and adopts it.
                    if (config.GlamourDresserItems is { Count: > 0 } items)
                    {
                        config.Dresser.Legacy = new DresserSnapshot
                        {
                            Items = items,
                            CapturedAt = config.GlamourDresserCapturedAt ?? DateTimeOffset.UtcNow,
                        };
                        log.Information("Dresser snapshot of {Count} items parked for the next character to log in", items.Count);
                    }

                    config.GlamourDresserItems = null;
                    config.GlamourDresserCapturedAt = null;
                    config.Version = 2;
                    break;

                default:
                    log.Warning("No migration step from configuration v{Version}; jumping to v{Current}", config.Version, Configuration.CurrentVersion);
                    config.Version = Configuration.CurrentVersion;
                    break;
            }
        }

        log.Information("Migrated configuration from v{From} to v{To}", from, config.Version);
        return true;
    }
}
