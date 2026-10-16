using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using NekoDex.Core.Farming;
using NekoDex.Data;

namespace NekoDex.UI;

/// <summary>Missing mounts grouped by the duty that drops them, with your clear count per duty. The farm shopping list.</summary>
public sealed class FarmTab
{
    private readonly CollectionTab mounts;
    private readonly DutyIndex duties;
    private readonly DutyClearTracker clears;
    private readonly PinController pins;
    private readonly ITextureProvider textureProvider;
    private bool hideHighEnd = true;

    public FarmTab(CollectionTab mounts, DutyIndex duties, DutyClearTracker clears, PinController pins, ITextureProvider textureProvider)
    {
        this.mounts = mounts;
        this.duties = duties;
        this.clears = clears;
        this.pins = pins;
        this.textureProvider = textureProvider;
    }

    public void Draw()
    {
        ImGui.Checkbox(Strings.Get("Farm_HideHighEnd", "Hide Savage and Ultimate"), ref hideHighEnd);
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(Strings.Get("Farm_Help", "Missing mounts whose unlock data names a duty, grouped by duty. The count is how many times this character has completed the duty since NekoDex started counting."));

        var groups = FarmPlanner.Group(mounts.Views, duties.Find, hideHighEnd);
        if (groups.Count == 0)
        {
            ImGui.TextDisabled(Strings.Get("Farm_Empty", "Nothing to farm: no missing mount names a duty."));
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        var iconSize = new Vector2(24, 24) * scale;
        using var child = ImRaii.Child("##farm", new Vector2(-1, -1), false);
        if (!child.Success)
        {
            return;
        }

        foreach (var group in groups)
        {
            using var id = ImRaii.PushId((int)group.Duty.Id);
            var count = clears.Count(group.Duty.Id);
            var header = $"{group.Duty.Name}  [{FarmLabels.For(group.Duty.Kind)}]  {string.Format(Strings.Get("Farm_Clears", "{0} clears"), count)}###{group.Duty.Id}";
            var open = ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(string.Format(Strings.Get("Farm_DutyTooltip", "Level {0}, item level {1}"), group.Duty.Level, group.Duty.ItemLevel));
            }

            if (!open)
            {
                continue;
            }

            using var indent = ImRaii.PushIndent(12f * scale);
            foreach (var view in group.Entries)
            {
                using var entryId = ImRaii.PushId((int)view.Entry.Id);
                var pinned = pins.IsPinned(mounts.Module.Id, view.Entry.Id);
                using (ImRaii.PushColor(ImGuiCol.Text, pinned ? Theme.PinColor : ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]))
                using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0)))
                {
                    if (ImGuiComponents.IconButton(pinned ? FontAwesomeIcon.Star : FontAwesomeIcon.StarHalfAlt))
                    {
                        pins.Toggle(mounts.Module.Id, view.Entry.Id);
                        mounts.Reset();
                    }
                }

                ImGui.SameLine();
                DrawIcon(view.Entry.IconId, iconSize);
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(view.Entry.Name);
            }

            if (count > 0)
            {
                if (ImGui.SmallButton(Strings.Get("Farm_ResetCount", "Reset count")))
                {
                    clears.Reset(group.Duty.Id);
                }
            }

            ImGui.Spacing();
        }
    }

    private void DrawIcon(uint iconId, Vector2 size)
    {
        if (iconId == 0 || !textureProvider.TryGetFromGameIcon(new GameIconLookup(iconId), out var texture))
        {
            ImGui.Dummy(size);
            return;
        }

        ImGui.Image(texture.GetWrapOrEmpty().Handle, size);
    }
}

internal static class FarmLabels
{
    public static string For(DutyKind kind) => kind switch
    {
        DutyKind.Dungeon => Strings.Get("Duty_Dungeon", "Dungeon"),
        DutyKind.Trial => Strings.Get("Duty_Trial", "Trial"),
        DutyKind.Raid => Strings.Get("Duty_Raid", "Raid"),
        DutyKind.Extreme => Strings.Get("Duty_Extreme", "Extreme"),
        DutyKind.Unreal => Strings.Get("Duty_Unreal", "Unreal"),
        DutyKind.Chaotic => Strings.Get("Duty_Chaotic", "Chaotic"),
        DutyKind.Savage => Strings.Get("Duty_Savage", "Savage"),
        DutyKind.Ultimate => Strings.Get("Duty_Ultimate", "Ultimate"),
        _ => Strings.Get("Duty_Other", "Other"),
    };
}
