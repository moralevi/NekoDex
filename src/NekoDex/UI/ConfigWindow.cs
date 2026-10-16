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

    public ConfigWindow(ConfigurationManager config)
        : base("NekoDex Settings###NekoDexConfig", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.config = config;
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
