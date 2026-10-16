using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using NekoDex.Core.Advisor;
using NekoDex.Core.Collections;
using NekoDex.Core.Outfits;
using NekoDex.Core.Unlock;
using NekoDex.Data;

namespace NekoDex.UI;

/// <summary>Per-slot next upgrade by item level for the current job and level. Recomputed when job, level or equipment changes.</summary>
public sealed class AdvisorTab
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly IPlayerState playerState;
    private readonly GearCatalog catalog;
    private readonly ItemOwnershipSources ownership;
    private readonly UnlockResolver unlockResolver;
    private readonly ITextureProvider textureProvider;

    private DateTime lastRefresh = DateTime.MinValue;
    private uint lastJob;
    private int lastLevel;
    private Dictionary<GearSlot, uint> lastEquipped = new();
    private IReadOnlyList<GearSuggestion> suggestions = [];
    private int averageItemLevel;

    public AdvisorTab(IPlayerState playerState, GearCatalog catalog, ItemOwnershipSources ownership, UnlockResolver unlockResolver, ITextureProvider textureProvider)
    {
        this.playerState = playerState;
        this.catalog = catalog;
        this.ownership = ownership;
        this.unlockResolver = unlockResolver;
        this.textureProvider = textureProvider;
    }

    public void Draw()
    {
        if (!playerState.ClassJob.IsValid)
        {
            ImGui.TextDisabled(Strings.Get("Advisor_NoJob", "No job detected."));
            return;
        }

        RefreshIfDue();

        var job = playerState.ClassJob.Value;
        ImGui.Text(string.Format(Strings.Get("Advisor_Header", "{0} level {1}, average item level {2}"), job.Abbreviation.ExtractText(), lastLevel, averageItemLevel));
        ImGui.TextDisabled(Strings.Get("Advisor_Hint", "Highest item level you can equip now per slot. Hover a suggestion for runners-up."));

        DrawTable();
    }

    private void RefreshIfDue()
    {
        var now = DateTime.UtcNow;
        var job = playerState.ClassJob.RowId;
        var level = playerState.Level;
        if (now - lastRefresh < RefreshInterval && job == lastJob && level == lastLevel)
        {
            return;
        }

        var equipped = EquippedGearReader.Read();
        var unchanged = job == lastJob && level == lastLevel && SameEquipment(equipped);
        lastRefresh = now;
        if (unchanged && suggestions.Count > 0)
        {
            return;
        }

        lastJob = job;
        lastLevel = level;
        lastEquipped = equipped;
        Recompute(job, level, equipped);
    }

    private bool SameEquipment(Dictionary<GearSlot, uint> equipped)
    {
        if (equipped.Count != lastEquipped.Count)
        {
            return false;
        }

        foreach (var (slot, id) in equipped)
        {
            if (!lastEquipped.TryGetValue(slot, out var previous) || previous != id)
            {
                return false;
            }
        }

        return true;
    }

    private void Recompute(uint job, int level, Dictionary<GearSlot, uint> equipped)
    {
        var result = new List<GearSuggestion>(GearAdvisor.Slots.Count);
        var equippedPieces = new List<GearCandidate?>();

        foreach (var slot in GearAdvisor.Slots)
        {
            GearCandidate? current = equipped.TryGetValue(slot, out var equippedId) && catalog.Find(equippedId) is { } item
                ? ToCandidate(item, slot, job)
                : null;
            equippedPieces.Add(current);

            // Off hand only matters for jobs that use one; skip it when nothing is equipped there.
            if (slot == GearSlot.OffHand && current is null)
            {
                continue;
            }

            var candidates = Candidates(slot, job);
            result.Add(GearAdvisor.Suggest(slot, current, candidates, level));
        }

        suggestions = result;
        averageItemLevel = GearAdvisor.AverageItemLevel(equippedPieces);
    }

    private IEnumerable<GearCandidate> Candidates(GearSlot slot, uint job)
    {
        foreach (var item in catalog.ForSlot(slot))
        {
            if (item.ItemLevel > 0 && catalog.JobCanUse(item.ClassJobCategoryId, job))
            {
                yield return ToCandidate(item, slot, job);
            }
        }
    }

    private GearCandidate ToCandidate(GearItem item, GearSlot slot, uint job)
        => new(item.ItemId, item.Name, item.IconId, slot, item.ItemLevel, item.EquipLevel, catalog.JobCanUse(item.ClassJobCategoryId, job));

    private void DrawTable()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var iconSize = new Vector2(24, 24) * scale;

        using var table = ImRaii.Table("##advisor", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY, new Vector2(-1, -1));
        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn(Strings.Get("Outfit_Slot", "Slot"), ImGuiTableColumnFlags.WidthFixed, 80 * scale);
        ImGui.TableSetupColumn(Strings.Get("Advisor_Equipped", "Equipped"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(Strings.Get("Advisor_Upgrade", "Next upgrade"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(Strings.Get("Advisor_Source", "Source"), ImGuiTableColumnFlags.WidthFixed, 110 * scale);
        ImGui.TableSetupColumn("##link", ImGuiTableColumnFlags.WidthFixed, 30 * scale);
        ImGui.TableHeadersRow();

        foreach (var suggestion in suggestions)
        {
            using var id = ImRaii.PushId((int)suggestion.Slot);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(GearLabels.For(suggestion.Slot));

            ImGui.TableNextColumn();
            DrawPiece(suggestion.Equipped, iconSize, Strings.Get("Advisor_Empty", "(empty)"));

            ImGui.TableNextColumn();
            var best = suggestion.Best;
            if (best is null)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Theme.OwnedColor, Strings.Get("Advisor_UpToDate", "Nothing better available"));
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                continue;
            }

            DrawPiece(best, iconSize, string.Empty);
            if (ImGui.IsItemHovered() && suggestion.Upgrades.Count > 1)
            {
                using var tooltip = ImRaii.Tooltip();
                ImGui.Text(Strings.Get("Advisor_RunnersUp", "Also:"));
                for (var i = 1; i < suggestion.Upgrades.Count; i++)
                {
                    var alt = suggestion.Upgrades[i];
                    ImGui.BulletText($"{alt.Name}  i{alt.ItemLevel}");
                }
            }

            var entry = new CollectionEntry(best.ItemId, best.Name, best.IconId);
            var source = unlockResolver.Resolve("items", entry);
            var where = ownership.Locate(best.ItemId);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (where != ItemOwnership.Missing)
            {
                ImGui.TextColored(Theme.OwnedColor, string.Format(Strings.Get("Advisor_Owned", "You have it ({0})"), GearLabels.For(where)));
            }
            else if (source.IsUnknown)
            {
                ImGui.TextDisabled(UnlockLabels.For(source.Type));
            }
            else
            {
                ImGui.Text(source.Summary);
            }

            if (ImGui.IsItemHovered() && !source.IsUnknown)
            {
                ImGui.SetTooltip(string.Join("\n", source.DetailLines));
            }

            ImGui.TableNextColumn();
            if (source.Url is not null)
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.ExternalLinkAlt))
                {
                    Util.OpenLink(source.Url);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Get("Tooltip_OpenOnWebShort", "Open on web"));
                }
            }
        }
    }

    private void DrawPiece(GearCandidate? piece, Vector2 iconSize, string emptyLabel)
    {
        ImGui.AlignTextToFramePadding();
        if (piece is null)
        {
            ImGui.TextDisabled(emptyLabel);
            return;
        }

        DrawIcon(piece.IconId, iconSize);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text($"{piece.Name}  i{piece.ItemLevel}");
    }

    private void DrawIcon(uint iconId, Vector2 size)
    {
        if (iconId == 0)
        {
            ImGui.Dummy(size);
            return;
        }

        // Icon ids come from sheets and offsets; a missing texture must never take the whole window down.
        if (!textureProvider.TryGetFromGameIcon(new GameIconLookup(iconId), out var texture))
        {
            ImGui.Dummy(size);
            return;
        }

        ImGui.Image(texture.GetWrapOrEmpty().Handle, size);
    }
}
