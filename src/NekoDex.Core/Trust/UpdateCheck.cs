namespace NekoDex.Core.Trust;

public enum UpdateStatus
{
    UpToDate,
    NewerPluginAvailable,
    NewerDataAvailable,
    Unknown,
}

public sealed record UpdateCheckResult(UpdateStatus Status, string? RemotePluginVersion, string? RemoteDataVersion);

/// <summary>Pure comparison of what is running against what the repository publishes. Fetching is the caller's job.</summary>
public static class UpdateCheck
{
    /// <summary>
    /// <paramref name="remotePluginVersion"/> is the pluginmaster's AssemblyVersion; <paramref name="remoteDataVersion"/> is the
    /// one-line unlocks.version file (yyyy-MM-dd). A newer plugin wins over newer data because the plugin ships the data.
    /// </summary>
    public static UpdateCheckResult Compare(string localPluginVersion, string? localDataVersion, string? remotePluginVersion, string? remoteDataVersion)
    {
        var plugin = Trim(remotePluginVersion);
        var data = Trim(remoteDataVersion);
        if (plugin is null && data is null)
        {
            return new UpdateCheckResult(UpdateStatus.Unknown, null, null);
        }

        if (plugin is not null && Version.TryParse(plugin, out var remote) && Version.TryParse(localPluginVersion, out var local) && remote > local)
        {
            return new UpdateCheckResult(UpdateStatus.NewerPluginAvailable, plugin, data);
        }

        if (data is not null && localDataVersion is not null && string.CompareOrdinal(data, localDataVersion) > 0)
        {
            return new UpdateCheckResult(UpdateStatus.NewerDataAvailable, plugin, data);
        }

        return new UpdateCheckResult(UpdateStatus.UpToDate, plugin, data);
    }

    /// <summary>Pulls AssemblyVersion out of a pluginmaster JSON array without a full model.</summary>
    public static string? AssemblyVersionFromPluginMaster(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            foreach (var plugin in doc.RootElement.EnumerateArray())
            {
                if (plugin.TryGetProperty("InternalName", out var name) && name.GetString() == "NekoDex" && plugin.TryGetProperty("AssemblyVersion", out var version))
                {
                    return version.GetString();
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
        }

        return null;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
