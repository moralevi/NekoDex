using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using NekoDex.Core.Resets;

namespace NekoDex.UI;

/// <summary>Compact countdowns, the unticked checklist items and the entries you are working on. Shown while the config flag is on.</summary>
public sealed class MiniWindow : ThemedWindow
{
    private readonly ConfigurationManager config;
    private readonly ChecklistController checklist;
    private readonly IReadOnlyList<CollectionTab> collections;
    private readonly PinController pins;
    private readonly Func<bool> characterReady;

    public MiniWindow(ConfigurationManager config, ChecklistController checklist, IReadOnlyList<CollectionTab> collections, PinController pins, Func<bool> characterReady)
        : base(config, "NekoDex Mini###NekoDexMini", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)
    {
        this.config = config;
        this.checklist = checklist;
        this.collections = collections;
        this.pins = pins;
        this.characterReady = characterReady;
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

        DrawOpenItems();
        DrawWorkingOn();
    }

    private void DrawOpenItems()
    {
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

    /// <summary>Pinned entries: missing ones as targets, owned ones with an unpin button so the list cleans itself up.</summary>
    private void DrawWorkingOn()
    {
        if (!characterReady())
        {
            return;
        }

        var any = false;
        foreach (var tab in collections)
        {
            if (tab.Module.UnavailableReason is not null)
            {
                continue;
            }

            foreach (var view in tab.PinnedViews())
            {
                if (!any)
                {
                    ImGui.Separator();
                    ImGui.TextDisabled(Strings.Get("Mini_WorkingOn", "Working on"));
                    any = true;
                }

                using var id = ImRaii.PushId($"{tab.Module.Id}:{view.Entry.Id}");
                if (view.Owned)
                {
                    ImGui.TextColored(Theme.OwnedColor, view.Entry.Name);
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Check))
                    {
                        pins.Toggle(tab.Module.Id, view.Entry.Id);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Strings.Get("Pin_Done", "Done! Click to unpin."));
                    }
                }
                else
                {
                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGui.Text(view.Entry.Name);
                    if (ImGui.IsItemHovered() && !view.Source.IsUnknown)
                    {
                        ImGui.SetTooltip(view.Source.Summary);
                    }
                }
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
