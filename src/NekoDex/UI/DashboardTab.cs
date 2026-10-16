using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using NekoDex.Core.Checklist;
using NekoDex.Core.Resets;

namespace NekoDex.UI;

/// <summary>Reset countdowns, per-module progress and the user's checklist. All time maths lives in Core; this only draws.</summary>
public sealed class DashboardTab
{
    private readonly ChecklistController checklist;
    private readonly AutoChecklistController autoItems;
    private readonly IReadOnlyList<CollectionTab> collections;
    private readonly Func<bool> characterReady;
    private readonly TabNavigation navigation;
    private string newItemText = string.Empty;
    private ResetCadence newItemCadence = ResetCadence.Daily;

    public DashboardTab(ChecklistController checklist, AutoChecklistController autoItems, IReadOnlyList<CollectionTab> collections, Func<bool> characterReady, TabNavigation navigation)
    {
        this.checklist = checklist;
        this.autoItems = autoItems;
        this.collections = collections;
        this.characterReady = characterReady;
        this.navigation = navigation;
    }

    public void Draw()
    {
        checklist.ApplyResets();
        var now = checklist.NowUtc;

        // Checklist first: it is the part that changes every day.
        DrawChecklist();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawResets(now);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawProgressSummary();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawNewThisPatch();
    }

    /// <summary>Per module: entries added in the newest patch the data knows, and how many are still missing. Click to open the tab filtered.</summary>
    private void DrawNewThisPatch()
    {
        ImGui.Text(Strings.Get("Dashboard_New", "New this patch"));

        if (!characterReady())
        {
            ImGui.TextDisabled(Strings.Get("State_NotLoggedIn", "Log in to a character to see your collections."));
            return;
        }

        var any = false;
        foreach (var tab in collections)
        {
            if (tab.Module.UnavailableReason is not null || tab.NewThisPatch() is not { } stats || stats.Total == 0)
            {
                continue;
            }

            any = true;
            using var id = ImRaii.PushId(tab.Module.Id);
            var label = stats.Missing == 0
                ? string.Format(Strings.Get("Dashboard_NewRowDone", "{0}: {1} new in {2}, all owned"), tab.Module.DisplayName, stats.Total, stats.Patch)
                : string.Format(Strings.Get("Dashboard_NewRow", "{0}: {1} new in {2}, {3} missing"), tab.Module.DisplayName, stats.Total, stats.Patch, stats.Missing);
            if (ImGui.Selectable(label))
            {
                tab.ShowLatestOnly();
                navigation.RequestedModuleId = tab.Module.Id;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Get("Dashboard_NewHint", "Open the tab filtered to this patch."));
            }
        }

        if (!any)
        {
            ImGui.TextDisabled(Strings.Get("Dashboard_NewNone", "Nothing to show; the unlock data has no patch information for the loaded collections."));
        }
    }

    private static void DrawResets(DateTimeOffset now)
    {
        ImGui.Text(Strings.Get("Dashboard_Resets", "Resets"));

        using var table = ImRaii.Table("##resets", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##countdown", ImGuiTableColumnFlags.WidthFixed, 110 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##local", ImGuiTableColumnFlags.WidthFixed, 130 * ImGuiHelpers.GlobalScale);

        foreach (var reset in KnownResets.All)
        {
            var next = reset.NextAfter(now);
            var local = ResetClock.ToLocal(next, TimeZoneInfo.Local);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(ResetLabels.For(reset));
            ImGui.TableNextColumn();
            ImGui.Text(ResetClock.FormatCountdown(next - now));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(reset.Day is null ? local.ToString("HH:mm") : local.ToString("ddd HH:mm"));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(string.Format(Strings.Get("Reset_LocalTooltip", "{0:f} your time ({1:HH:mm} UTC)"), local, next.UtcDateTime));
            }
        }
    }

    private void DrawProgressSummary()
    {
        ImGui.Text(Strings.Get("Dashboard_Progress", "Collections"));

        if (!characterReady())
        {
            ImGui.TextDisabled(Strings.Get("State_NotLoggedIn", "Log in to a character to see your collections."));
            return;
        }

        using var table = ImRaii.Table("##progress", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupColumn("##module", ImGuiTableColumnFlags.WidthFixed, 110 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##bar", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##count", ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale);

        foreach (var tab in collections)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(tab.Module.DisplayName);

            ImGui.TableNextColumn();
            if (tab.Module.UnavailableReason is { } reason)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled(Strings.Get("Dashboard_Unavailable", "not loaded"));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(reason);
                }

                ImGui.TableNextColumn();
                continue;
            }

            var progress = tab.CurrentSnapshot().Progress;
            ImGui.ProgressBar(progress.Fraction, new Vector2(-1, 0), string.Empty);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(string.Format(Strings.Get("Progress_Overlay", "{0} / {1}"), progress.Owned, progress.Total));
        }
    }

    private void DrawChecklist()
    {
        ImGui.Text(Strings.Get("Dashboard_Checklist", "Checklist"));

        DrawAddRow();
        ImGui.Spacing();

        var anyAuto = false;
        using var table = ImRaii.Table("##checklist", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg);
        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupColumn("##item", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##cadence", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##delete", ImGuiTableColumnFlags.WidthFixed, 28 * ImGuiHelpers.GlobalScale);

        // Game-reported items first. They cannot be ticked by hand; the game decides.
        foreach (var (definition, reading) in autoItems.Visible())
        {
            anyAuto = true;
            using var autoId = ImRaii.PushId(definition.Id);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var done = reading?.Complete ?? false;
            using (ImRaii.Disabled())
            {
                ImGui.Checkbox(definition.Name, ref done);
            }

            if (reading?.Detail is { } detail)
            {
                ImGui.SameLine();
                ImGui.TextDisabled(detail);
            }
            else if (reading is null)
            {
                ImGui.SameLine();
                ImGui.TextDisabled(Strings.Get("Auto_NotRead", "(not read yet)"));
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled(ResetLabels.For(definition.Cadence));

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled(Strings.Get("Auto_Badge", "auto"));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Get("Auto_Tooltip", "Read from the game. Choose which auto items to show in settings."));
            }
        }

        if (!anyAuto && checklist.Items.Count == 0)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextDisabled(Strings.Get("Checklist_Empty", "No items yet. Add one above; daily and weekly items untick themselves at reset."));
            return;
        }

        ChecklistItem? toRemove = null;
        foreach (var item in checklist.Items)
        {
            using var id = ImRaii.PushId(item.Id.ToString());
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var isChecked = item.Checked;
            if (ImGui.Checkbox(item.Text, ref isChecked))
            {
                checklist.SetChecked(item, isChecked);
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled(ResetLabels.For(item.Cadence));

            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                toRemove = item;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Get("Checklist_Delete", "Delete"));
            }
        }

        if (toRemove is not null)
        {
            checklist.Remove(toRemove);
        }
    }

    private void DrawAddRow()
    {
        var scale = ImGuiHelpers.GlobalScale;

        ImGui.SetNextItemWidth(-(150 + 60) * scale);
        var submitted = ImGui.InputTextWithHint("##newItem", Strings.Get("Checklist_NewHint", "New item"), ref newItemText, 120, ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(90 * scale);
        using (var combo = ImRaii.Combo("##cadence", ResetLabels.For(newItemCadence)))
        {
            if (combo.Success)
            {
                foreach (var cadence in Enum.GetValues<ResetCadence>())
                {
                    if (ImGui.Selectable(ResetLabels.For(cadence), cadence == newItemCadence))
                    {
                        newItemCadence = cadence;
                    }
                }
            }
        }

        ImGui.SameLine();
        if (ImGui.Button(Strings.Get("Checklist_Add", "Add")) || submitted)
        {
            checklist.Add(newItemText, newItemCadence);
            newItemText = string.Empty;
        }
    }
}
