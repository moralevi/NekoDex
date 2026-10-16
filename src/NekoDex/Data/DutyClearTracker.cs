using System;
using Dalamud.Game.DutyState;
using Dalamud.Plugin.Services;
using NekoDex.Core.Farming;

namespace NekoDex.Data;

/// <summary>Counts duty completions for the logged-in character from Dalamud's duty state events. Read-only; nothing is sent to the game.</summary>
public sealed class DutyClearTracker : IDisposable
{
    private readonly IDutyState dutyState;
    private readonly IPlayerState playerState;
    private readonly ConfigurationManager config;
    private readonly IPluginLog log;

    public DutyClearTracker(IDutyState dutyState, IPlayerState playerState, ConfigurationManager config, IPluginLog log)
    {
        this.dutyState = dutyState;
        this.playerState = playerState;
        this.config = config;
        this.log = log;
        dutyState.DutyCompleted += OnDutyCompleted;
    }

    public KillCounter Counter => config.Current.DutyClears;

    public ulong ContentId => playerState.IsLoaded ? playerState.ContentId : 0;

    public int Count(uint dutyId) => Counter.Count(ContentId, dutyId);

    public void Reset(uint dutyId)
    {
        if (Counter.Reset(ContentId, dutyId))
        {
            config.Save();
        }
    }

    public void Dispose() => dutyState.DutyCompleted -= OnDutyCompleted;

    private void OnDutyCompleted(IDutyStateEventArgs args)
    {
        var dutyId = dutyState.ContentFinderCondition.RowId;
        var contentId = ContentId;
        if (dutyId == 0 || contentId == 0)
        {
            return;
        }

        var count = Counter.Record(contentId, dutyId);
        config.Save();
        log.Debug("Duty {Duty} completed, clear #{Count} for this character", dutyId, count);
    }
}
