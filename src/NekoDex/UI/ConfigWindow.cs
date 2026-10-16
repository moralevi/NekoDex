using System;
using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using NekoDex.Core.Transfer;
using NekoDex.Core.Unlock;

namespace NekoDex.UI;

public sealed class ConfigWindow : ThemedWindow
{
    private readonly ConfigurationManager config;
    private readonly Action<bool> setMiniWindowShown;
    private readonly DatasetUnlockSourceProvider? dataset;
    private readonly string pluginVersion;
    private readonly AutoChecklistController autoItems;
    private readonly UserDataService userData;
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly Action<string?> setLanguage;
    private readonly FileDialogManager fileDialogs = new();
    private UserDataFile? pendingImport;
    private string? pendingImportName;
    private ImportMode importMode = ImportMode.Merge;
    private string dataStatus = string.Empty;

    public ConfigWindow(ConfigurationManager config, Action<bool> setMiniWindowShown, DatasetUnlockSourceProvider? dataset, string pluginVersion, AutoChecklistController autoItems, UserDataService userData, IDalamudPluginInterface pluginInterface, Action<string?> setLanguage)
        : base(config, "NekoDex Settings###NekoDexConfig", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.config = config;
        this.setMiniWindowShown = setMiniWindowShown;
        this.dataset = dataset;
        this.pluginVersion = pluginVersion;
        this.autoItems = autoItems;
        this.userData = userData;
        this.pluginInterface = pluginInterface;
        this.setLanguage = setLanguage;
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

        Section(Strings.Get("Config_AutoItems", "Automatic checklist items"));

        Hint(Strings.Get("Config_AutoItemsHint", "Read from the game; they tick themselves."));
        foreach (var definition in autoItems.Definitions)
        {
            var enabled = autoItems.IsEnabled(definition.Id);
            if (ImGui.Checkbox($"{definition.Name}##{definition.Id}", ref enabled))
            {
                autoItems.SetEnabled(definition.Id, enabled);
            }
        }

        Section(Strings.Get("Config_Appearance", "Appearance"));

        var neko = current.UseNekoTheme;
        if (ImGui.Checkbox(Strings.Get("Config_NekoTheme", "Neko theme"), ref neko))
        {
            current.UseNekoTheme = neko;
            config.Save();
        }

        Hint(Strings.Get("Config_NekoThemeHint", "Off uses your Dalamud style unchanged. Applies immediately."));

        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        var followLabel = string.Format(Strings.Get("Config_LanguageFollow", "Follow Dalamud ({0})"), Strings.DisplayName(pluginInterface.UiLanguage));
        var currentLabel = current.LanguageOverride is { } code ? Strings.DisplayName(code) : followLabel;
        using (var combo = ImRaii.Combo(Strings.Get("Config_Language", "Language"), currentLabel))
        {
            if (combo.Success)
            {
                if (ImGui.Selectable(followLabel, current.LanguageOverride is null))
                {
                    setLanguage(null);
                }

                foreach (var available in Strings.Available(pluginInterface))
                {
                    if (ImGui.Selectable(Strings.DisplayName(available), current.LanguageOverride == available))
                    {
                        setLanguage(available);
                    }
                }
            }
        }

        Hint(Strings.Get("Config_LanguageHint", "Dalamud has no Swedish, so pick it here. Applies immediately."));

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

        Section(Strings.Get("Config_Data", "Your data"));

        Hint(Strings.Get("Config_DataHint", "Pins, checklist and auto item choices as one JSON file."));
        if (ImGui.Button(Strings.Get("Data_Export", "Export...")))
        {
            fileDialogs.SaveFileDialog(Strings.Get("Data_ExportTitle", "Export NekoDex data"), ".json", userData.DefaultFileName(), ".json", (ok, path) =>
            {
                if (ok)
                {
                    dataStatus = userData.Export(path);
                }
            });
        }

        ImGui.SameLine();
        if (ImGui.Button(Strings.Get("Data_Import", "Import...")))
        {
            fileDialogs.OpenFileDialog(Strings.Get("Data_ImportTitle", "Import NekoDex data"), ".json", (ok, path) =>
            {
                if (!ok)
                {
                    return;
                }

                var (file, error) = userData.Load(path);
                pendingImport = file;
                pendingImportName = Path.GetFileName(path);
                dataStatus = error is null ? string.Empty : string.Format(Strings.Get("Data_ImportRefused", "Cannot import {0}: {1}"), Path.GetFileName(path), error);
            });
        }

        if (pendingImport is { } pending)
        {
            var counts = UserDataTransfer.Preview(pending);
            Hint(string.Format(
                Strings.Get("Data_Preview", "{0} contains {1} pins, {2} checklist items, {3} auto items (exported by NekoDex {4})."),
                pendingImportName, counts.Pins, counts.Checklist, counts.AutoItems, pending.PluginVersion));

            using (ImRaii.PushIndent(20f * ImGuiHelpers.GlobalScale))
            {
                if (ImGui.RadioButton(Strings.Get("Data_Merge", "Merge (keep mine on conflicts)"), importMode == ImportMode.Merge))
                {
                    importMode = ImportMode.Merge;
                }

                ImGui.SameLine();
                if (ImGui.RadioButton(Strings.Get("Data_Replace", "Replace sections in the file"), importMode == ImportMode.Replace))
                {
                    importMode = ImportMode.Replace;
                }

                if (ImGui.Button(Strings.Get("Data_Apply", "Apply import")))
                {
                    dataStatus = userData.Import(pending, importMode);
                    pendingImport = null;
                }

                ImGui.SameLine();
                if (ImGui.Button(Strings.Get("Data_Cancel", "Cancel")))
                {
                    pendingImport = null;
                    dataStatus = string.Empty;
                }
            }
        }

        if (dataStatus.Length > 0)
        {
            Hint(dataStatus);
        }

        Section(Strings.Get("Config_About", "About"));

        ImGui.TextDisabled(string.Format(Strings.Get("Config_Version", "NekoDex {0}, config v{1}"), pluginVersion, current.Version));
        ImGui.TextDisabled(dataset is null
            ? Strings.Get("Config_NoDataset", "Bundled unlock data: unavailable")
            : string.Format(Strings.Get("Config_Dataset", "Unlock data {0} from {1}, for game {2}"), dataset.DataVersion, dataset.Source, dataset.GameVersion));

        fileDialogs.Draw();
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
