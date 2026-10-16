using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace NekoDex;

/// <summary>Owns loading, migrating and saving the <see cref="Configuration"/> so nothing else needs the plugin interface for it.</summary>
public sealed class ConfigurationManager
{
    private readonly IDalamudPluginInterface pluginInterface;

    public ConfigurationManager(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        Current = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        if (ConfigurationMigrator.Migrate(Current, log))
        {
            Save();
        }
    }

    public Configuration Current { get; }

    public void Save() => pluginInterface.SavePluginConfig(Current);
}
