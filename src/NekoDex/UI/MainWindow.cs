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
    private readonly DashboardTab dashboard;
    private readonly IReadOnlyList<CollectionTab> tabs;
    private readonly OutfitsTab outfits;

    public MainWindow(IClientState clientState, IPlayerState playerState, DashboardTab dashboard, IReadOnlyList<CollectionTab> tabs, OutfitsTab outfits, Action openSettings)
        : base("NekoDex###NekoDexMain")
    {
        this.clientState = clientState;
        this.playerState = playerState;
        this.dashboard = dashboard;
        this.tabs = tabs;
        this.outfits = outfits;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 420),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Size = new Vector2(620, 720);
        SizeCondition = ImGuiCond.FirstUseEver;

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new Vector2(2, 1),
            Click = _ => openSettings(),
            ShowTooltip = () => ImGui.SetTooltip(Strings.Get("Tooltip_Settings", "NekoDex settings")),
        });
    }

    /// <summary>Drop every cached snapshot. Called on login and logout so a different character never sees stale data.</summary>
    public void ResetCollections()
    {
        foreach (var tab in tabs)
        {
            tab.Reset();
        }
    }

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("##main");
        if (!tabBar.Success)
        {
            return;
        }

        // The dashboard needs no character: resets are fixed in UTC and the checklist is per plugin config.
        using (var item = ImRaii.TabItem($"{Strings.Get("Tab_Dashboard", "Dashboard")}###dashboard"))
        {
            if (item.Success)
            {
                dashboard.Draw();
            }
        }

        foreach (var tab in tabs)
        {
            using var item = ImRaii.TabItem($"{tab.Module.DisplayName}###{tab.Module.Id}");
            if (item.Success && DrawCharacterHeader())
            {
                tab.Draw();
            }
        }

        using (var item = ImRaii.TabItem($"{Strings.Get("Tab_Outfits", "Outfits")}###outfits"))
        {
            if (item.Success && DrawCharacterHeader())
            {
                outfits.Draw();
            }
        }
    }

    /// <summary>Every "game not ready" state renders a message instead of touching character data. Returns true when character data may be read.</summary>
    private bool DrawCharacterHeader()
    {
        if (!clientState.IsLoggedIn)
        {
            ImGui.TextDisabled(Strings.Get("State_NotLoggedIn", "Log in to a character to see your collections."));
            return false;
        }

        if (!playerState.IsLoaded)
        {
            ImGui.TextDisabled(Strings.Get("State_Loading", "Loading character data..."));
            return false;
        }

        ImGui.Text(playerState.CharacterName);
        if (playerState.HomeWorld.IsValid)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"@ {playerState.HomeWorld.Value.Name}");
        }

        return true;
    }
}
