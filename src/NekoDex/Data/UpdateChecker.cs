using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using NekoDex.Core.Trust;

namespace NekoDex.Data;

/// <summary>
/// The plugin's only network access: on the user's click, two small GETs against the public repository
/// (the pluginmaster and the one-line dataset version file). Never automatic.
/// </summary>
public sealed class UpdateChecker : IDisposable
{
    private const string PluginMasterUrl = "https://raw.githubusercontent.com/moralevi/NekoDex/main/repo.json";
    private const string DataVersionUrl = "https://raw.githubusercontent.com/moralevi/NekoDex/main/src/NekoDex/Data/unlocks.version";

    private readonly IPluginLog log;
    private readonly string pluginVersion;
    private readonly string? dataVersion;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private CancellationTokenSource? inFlight;

    public UpdateChecker(IPluginLog log, string pluginVersion, string? dataVersion)
    {
        this.log = log;
        this.pluginVersion = pluginVersion;
        this.dataVersion = dataVersion;
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"NekoDex/{pluginVersion}");
    }

    public bool IsChecking => inFlight is not null;

    /// <summary>Last outcome for the settings window; null until a check ran.</summary>
    public UpdateCheckResult? LastResult { get; private set; }

    public string? LastError { get; private set; }

    public DateTime? LastCheckedUtc { get; private set; }

    public void Check()
    {
        if (inFlight is not null)
        {
            return;
        }

        inFlight = new CancellationTokenSource();
        _ = RunAsync(inFlight.Token);
    }

    public void Dispose()
    {
        inFlight?.Cancel();
        http.Dispose();
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            string? remotePlugin = null;
            string? remoteData = null;
            try
            {
                remotePlugin = UpdateCheck.AssemblyVersionFromPluginMaster(await http.GetStringAsync(PluginMasterUrl, token));
            }
            catch (HttpRequestException ex)
            {
                log.Information("Update check: pluginmaster not reachable ({Status})", ex.StatusCode?.ToString() ?? "no response");
            }

            try
            {
                remoteData = await http.GetStringAsync(DataVersionUrl, token);
            }
            catch (HttpRequestException ex)
            {
                log.Information("Update check: data version not reachable ({Status})", ex.StatusCode?.ToString() ?? "no response");
            }

            LastResult = UpdateCheck.Compare(pluginVersion, dataVersion, remotePlugin, remoteData);
            LastError = LastResult.Status == UpdateStatus.Unknown ? "Repository not reachable (is it public yet?)" : null;
            log.Information("Update check: {Status} (remote plugin {Plugin}, remote data {Data})", LastResult.Status, remotePlugin ?? "none", remoteData?.Trim() ?? "none");
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {
            LastError = "Timed out.";
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            log.Warning(ex, "Update check failed");
        }
        finally
        {
            LastCheckedUtc = DateTime.UtcNow;
            inFlight?.Dispose();
            inFlight = null;
        }
    }
}
