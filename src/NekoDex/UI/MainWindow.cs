using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace NekoDex.UI;

public sealed class MainWindow : Window
{
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;
    private readonly IReadOnlyList<CollectionTab> tabs;

    public MainWindow(IClientState clientState, IPlayerState playerState, IReadOnlyList<CollectionTab> tabs, Action openSettings)
        : base("NekoDex###NekoDexMain")
    {
        this.clientState = clientState;
        this.playerState = playerState;
        this.tabs = tabs;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 360),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Size = new Vector2(560, 640);
        SizeCondition = ImGuiCond.FirstUseEver;

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new Vector2(2, 1),
            Click = _ => openSettings(),
            ShowTooltip = () => ImGui.SetTooltip(Strings.Get("Tooltip_Settings", "NekoDex settings")),
        });
    }

    /// <summary>Drop every cached snapshot. Called on logout so a different character never sees stale data.</summary>
    public void ResetCollections()
    {
        foreach (var tab in tabs)
        {
            tab.Reset();
        }
    }

    public override void Draw()
    {
        // Every "game not ready" state renders a message instead of touching character data.
        if (!clientState.IsLoggedIn)
        {
            ImGui.TextDisabled(Strings.Get("State_NotLoggedIn", "Log in to a character to see your collections."));
            return;
        }

        if (!playerState.IsLoaded)
        {
            ImGui.TextDisabled(Strings.Get("State_Loading", "Loading character data..."));
            return;
        }

        ImGui.Text(playerState.CharacterName);
        if (playerState.HomeWorld.IsValid)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"@ {playerState.HomeWorld.Value.Name}");
        }

        using var tabBar = ImRaii.TabBar("##modules");
        if (!tabBar.Success)
        {
            return;
        }

        foreach (var tab in tabs)
        {
            using var item = ImRaii.TabItem($"{tab.Module.DisplayName}###{tab.Module.Id}");
            if (item.Success)
            {
                tab.Draw();
            }
        }
    }
}
