using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using NekoDex.Core.Unlock;

namespace NekoDex.UI;

public sealed class ConfigWindow : Window
{
    private readonly ConfigurationManager config;
    private readonly Action<bool> setMiniWindowShown;

    public ConfigWindow(ConfigurationManager config, Action<bool> setMiniWindowShown)
        : base("NekoDex Settings###NekoDexConfig", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.config = config;
        this.setMiniWindowShown = setMiniWindowShown;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(360, 0) };
    }

    public override void Draw()
    {
        var current = config.Current;

        var openOnLogin = current.OpenMainWindowOnLogin;
        if (ImGui.Checkbox(Strings.Get("Config_OpenOnLogin", "Open NekoDex when a character logs in"), ref openOnLogin))
        {
            current.OpenMainWindowOnLogin = openOnLogin;
            config.Save();
        }

        var showMini = current.ShowMiniWindow;
        if (ImGui.Checkbox(Strings.Get("Config_ShowMini", "Show the mini window (countdowns and open to-dos)"), ref showMini))
        {
            setMiniWindowShown(showMini);
        }

        ImGui.Spacing();
        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo(Strings.Get("Config_LinkSite", "Website for unlock info"), UnlockLinks.SiteName(current.LinkSite)))
        {
            if (combo.Success)
            {
                foreach (var site in Enum.GetValues<UnlockLinkSite>())
                {
                    if (ImGui.Selectable(UnlockLinks.SiteName(site), site == current.LinkSite))
                    {
                        current.LinkSite = site;
                        config.Save();
                    }
                }
            }
        }

        ImGui.Spacing();
        ImGui.TextDisabled($"Config v{current.Version}");
    }
}
