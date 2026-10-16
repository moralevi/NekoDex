using Dalamud.Plugin.Services;

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
                    // v1 -> v2 used to move the Glamour Dresser snapshot to per-character storage. That feature was
                    // removed, so the old fields are simply ignored on load and vanish on save.
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
