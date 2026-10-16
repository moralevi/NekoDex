using System;
using Dalamud.Plugin.Services;
using NekoDex.Core.Resets;
using NekoDex.UI;

namespace NekoDex;

/// <summary>Shows a toast when the daily or weekly reset passes while a character is loaded. Ticks once a second from the framework update.</summary>
public sealed class ResetNotifier : IDisposable
{
    private static readonly ResetDefinition[] Watched = [KnownResets.Daily, KnownResets.Weekly];
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly IFramework framework;
    private readonly IToastGui toasts;
    private readonly ConfigurationManager config;
    private readonly Func<bool> characterReady;
    private readonly ResetWatcher watcher = new();
    private DateTime lastTick = DateTime.MinValue;

    public ResetNotifier(IFramework framework, IToastGui toasts, ConfigurationManager config, Func<bool> characterReady)
    {
        this.framework = framework;
        this.toasts = toasts;
        this.config = config;
        this.characterReady = characterReady;
        framework.Update += OnUpdate;
    }

    /// <summary>Forget the last boundary so the next tick primes instead of announcing a reset that happened while logged out.</summary>
    public void Reset() => watcher.Clear();

    public void Dispose() => framework.Update -= OnUpdate;

    private void OnUpdate(IFramework _)
    {
        var now = DateTime.UtcNow;
        if (now - lastTick < Interval)
        {
            return;
        }

        lastTick = now;
        if (!characterReady())
        {
            watcher.Clear();
            return;
        }

        foreach (var reset in watcher.Tick(Watched, DateTimeOffset.UtcNow))
        {
            if (config.Current.ResetToasts)
            {
                toasts.ShowNormal(string.Format(Strings.Get("Toast_Reset", "NekoDex: {0} has passed."), ResetLabels.For(reset)));
            }
        }
    }
}
