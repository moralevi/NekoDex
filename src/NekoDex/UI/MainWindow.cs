using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace NekoDex.UI;

public sealed class MainWindow : Window
{
    private readonly IClientState clientState;
    private readonly IPlayerState playerState;

    public MainWindow(IClientState clientState, IPlayerState playerState, Action openSettings)
        : base("NekoDex###NekoDexMain")
    {
        this.clientState = clientState;
        this.playerState = playerState;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new Vector2(2, 1),
            Click = _ => openSettings(),
            ShowTooltip = () => ImGui.SetTooltip("NekoDex settings"),
        });
    }

    public override void Draw()
    {
        // Every "game not ready" state renders a message instead of touching character data.
        if (!clientState.IsLoggedIn)
        {
            ImGui.TextDisabled("Log in to a character to see your collections.");
            return;
        }

        if (!playerState.IsLoaded)
        {
            ImGui.TextDisabled("Loading character data...");
            return;
        }

        ImGui.Text(playerState.CharacterName);
        if (playerState.HomeWorld.IsValid)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"@ {playerState.HomeWorld.Value.Name}");
        }

        ImGui.Separator();
        ImGui.TextDisabled("Mounts will appear here in the next phase.");
    }
}
