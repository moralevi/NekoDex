using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using NekoDex.Core.Resets;

namespace NekoDex.UI;

/// <summary>Compact countdowns and the unticked checklist items. Shown while the config flag is on.</summary>
public sealed class MiniWindow : ThemedWindow
{
    private readonly ConfigurationManager config;
    private readonly ChecklistController checklist;

    public MiniWindow(ConfigurationManager config, ChecklistController checklist)
        : base(config, "NekoDex Mini###NekoDexMini", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)
    {
        this.config = config;
        this.checklist = checklist;
        RespectCloseHotkey = false;
        IsOpen = config.Current.ShowMiniWindow;
    }

    public void SetShown(bool shown)
    {
        if (config.Current.ShowMiniWindow != shown)
        {
            config.Current.ShowMiniWindow = shown;
            config.Save();
        }

        IsOpen = shown;
    }

    public override void OnClose()
    {
        // Closing with the X turns the option off so it does not come back next launch unasked.
        SetShown(false);
    }

    public override void Draw()
    {
        checklist.ApplyResets();
        var now = checklist.NowUtc;

        using (var table = ImRaii.Table("##mini", 2, ImGuiTableFlags.SizingFixedFit))
        {
            if (table.Success)
            {
                Row(ResetLabels.For(KnownResets.Daily), KnownResets.Daily, now);
                Row(ResetLabels.For(KnownResets.Weekly), KnownResets.Weekly, now);
            }
        }

        var any = false;
        foreach (var item in checklist.Items)
        {
            if (item.Checked)
            {
                continue;
            }

            if (!any)
            {
                ImGui.Separator();
                any = true;
            }

            using var id = ImRaii.PushId(item.Id.ToString());
            var isChecked = false;
            if (ImGui.Checkbox(item.Text, ref isChecked))
            {
                checklist.SetChecked(item, true);
            }
        }
    }

    private static void Row(string label, ResetDefinition reset, DateTimeOffset now)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextDisabled(label);
        ImGui.TableNextColumn();
        ImGui.Text(ResetClock.FormatCountdown(reset.NextAfter(now) - now));
    }
}
