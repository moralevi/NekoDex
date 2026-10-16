using System.Collections.Generic;

namespace NekoDex;

/// <summary>Starred entries per module, stored in the configuration.</summary>
public sealed class PinController
{
    private readonly ConfigurationManager config;

    public PinController(ConfigurationManager config)
    {
        this.config = config;
    }

    public bool IsPinned(string moduleId, uint entryId)
        => config.Current.Pins.TryGetValue(moduleId, out var ids) && ids.Contains(entryId);

    public IReadOnlyList<uint> For(string moduleId)
        => config.Current.Pins.TryGetValue(moduleId, out var ids) ? ids : [];

    public void Toggle(string moduleId, uint entryId)
    {
        if (!config.Current.Pins.TryGetValue(moduleId, out var ids))
        {
            ids = new List<uint>();
            config.Current.Pins[moduleId] = ids;
        }

        if (!ids.Remove(entryId))
        {
            ids.Add(entryId);
        }

        if (ids.Count == 0)
        {
            config.Current.Pins.Remove(moduleId);
        }

        config.Save();
    }
}
