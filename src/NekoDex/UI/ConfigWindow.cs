using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using NekoDex.Core.Unlock;

namespace NekoDex.UI;

public sealed class ConfigWindow : ThemedWindow
{
    private readonly ConfigurationManager config;
    private readonly Action<bool> setMiniWindowShown;
    private readonly DatasetUnlockSourceProvider? dataset;
    private readonly string pluginVersion;

    public ConfigWindow(ConfigurationManager config, Action<bool> setMiniWindowShown, DatasetUnlockSourceProvider? dataset, string pluginVersion)
        : base(config, "NekoDex Settings###NekoDexConfig", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.config = config;
        this.setMiniWindowShown = setMiniWindowShown;
        this.dataset = dataset;
        this.pluginVersion = pluginVersion;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(380, 0) };
    }

    public override void Draw()
    {
        var current = config.Current;

        Section(Strings.Get("Config_General", "General"));

        var openOnLogin = current.OpenMainWindowOnLogin;
        if (ImGui.Checkbox(Strings.Get("Config_OpenOnLogin", "Open NekoDex when a character logs in"), ref openOnLogin))
        {
            current.OpenMainWindowOnLogin = openOnLogin;
            config.Save();
        }

        var showMini = current.ShowMiniWindow;
        if (ImGui.Checkbox(Strings.Get("Config_ShowMini", "Show the mini window"), ref showMini))
        {
            setMiniWindowShown(showMini);
        }

        Hint(Strings.Get("Config_ShowMiniHint", "Countdowns and open to-dos in a small panel. Also /nekodex mini."));

        Section(Strings.Get("Config_Appearance", "Appearance"));

        var neko = current.UseNekoTheme;
        if (ImGui.Checkbox(Strings.Get("Config_NekoTheme", "Neko theme"), ref neko))
        {
            current.UseNekoTheme = neko;
            config.Save();
        }

        Hint(Strings.Get("Config_NekoThemeHint", "Off uses your Dalamud style unchanged. Applies immediately."));

        Section(Strings.Get("Config_Links", "Web links"));

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

        Hint(Strings.Get("Config_LinkSiteHint", "Used by the open-on-web buttons. Falls back to the other site when one has no page for an entry."));

        Section(Strings.Get("Config_About", "About"));

        ImGui.TextDisabled(string.Format(Strings.Get("Config_Version", "NekoDex {0}, config v{1}"), pluginVersion, current.Version));
        ImGui.TextDisabled(dataset is null
            ? Strings.Get("Config_NoDataset", "Bundled unlock data: unavailable")
            : string.Format(Strings.Get("Config_Dataset", "Unlock data {0} from {1}, for game {2}"), dataset.DataVersion, dataset.Source, dataset.GameVersion));
    }

    private static void Section(string title)
    {
        ImGui.Spacing();
        ImGui.TextColored(Theme.Active ? new Vector4(0.93f, 0.55f, 0.65f, 1f) : ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark], title);
        ImGui.Separator();
    }

    private static void Hint(string text)
    {
        using var indent = ImRaii.PushIndent(20f * ImGuiHelpers.GlobalScale);
        ImGui.TextDisabled(text);
    }
}
