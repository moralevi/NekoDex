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
using NekoDex.Core.Outfits;
using NekoDex.Data;

namespace NekoDex.UI;

/// <summary>Outfit planner: named outfits, one item and two dyes per slot, with where each item is (dresser, armoire, bags) or that it is missing.</summary>
public sealed class OutfitsTab
{
    private const int PickerLimit = 150;

    private readonly ConfigurationManager config;
    private readonly GearCatalog catalog;
    private readonly StainCatalog stains;
    private readonly ItemOwnershipSources ownership;
    private readonly GlamourDresserReader dresser;
    private readonly ITextureProvider textureProvider;

    private Guid? selectedId;
    private string newOutfitName = string.Empty;
    private string renameBuffer = string.Empty;
    private GearSlot? pickingSlot;
    private string pickerSearch = string.Empty;
    private (GearSlot Slot, bool Secondary)? pickingDye;
    private string dyeSearch = string.Empty;

    public OutfitsTab(ConfigurationManager config, GearCatalog catalog, StainCatalog stains, ItemOwnershipSources ownership, GlamourDresserReader dresser, ITextureProvider textureProvider)
    {
        this.config = config;
        this.catalog = catalog;
        this.stains = stains;
        this.ownership = ownership;
        this.dresser = dresser;
        this.textureProvider = textureProvider;
    }

    public void Draw()
    {
        dresser.Refresh();

        var scale = ImGuiHelpers.GlobalScale;
        using (var left = ImRaii.Child("##outfitList", new Vector2(200 * scale, -1), true))
        {
            if (left.Success)
            {
                DrawOutfitList();
            }
        }

        ImGui.SameLine();
        using (var right = ImRaii.Child("##outfitDetail", new Vector2(-1, -1), true))
        {
            if (right.Success)
            {
                DrawSelectedOutfit();
            }
        }
    }

    private void DrawOutfitList()
    {
        ImGui.SetNextItemWidth(-1);
        var submitted = ImGui.InputTextWithHint("##newOutfit", Strings.Get("Outfit_NewHint", "New outfit name"), ref newOutfitName, 60, ImGuiInputTextFlags.EnterReturnsTrue);
        if (ImGui.Button(Strings.Get("Outfit_Add", "Add outfit"), new Vector2(-1, 0)) || submitted)
        {
            var name = newOutfitName.Trim();
            if (name.Length > 0)
            {
                var outfit = new Outfit { Name = name };
                config.Current.Outfits.Add(outfit);
                config.Save();
                Select(outfit);
                newOutfitName = string.Empty;
            }
        }

        ImGui.Separator();

        foreach (var outfit in config.Current.Outfits)
        {
            if (ImGui.Selectable($"{outfit.Name}##{outfit.Id}", selectedId == outfit.Id))
            {
                Select(outfit);
            }
        }

        if (config.Current.Outfits.Count == 0)
        {
            ImGui.TextDisabled(Strings.Get("Outfit_None", "No outfits yet."));
        }
    }

    private void Select(Outfit outfit)
    {
        selectedId = outfit.Id;
        renameBuffer = outfit.Name;
        pickingSlot = null;
        pickingDye = null;
    }

    private Outfit? Selected()
    {
        if (selectedId is not { } id)
        {
            return null;
        }

        foreach (var outfit in config.Current.Outfits)
        {
            if (outfit.Id == id)
            {
                return outfit;
            }
        }

        return null;
    }

    private void DrawSelectedOutfit()
    {
        var outfit = Selected();
        if (outfit is null)
        {
            ImGui.TextDisabled(Strings.Get("Outfit_SelectHint", "Select or add an outfit."));
            DrawSourcesFooter();
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetNextItemWidth(220 * scale);
        if (ImGui.InputText("##rename", ref renameBuffer, 60, ImGuiInputTextFlags.EnterReturnsTrue) && renameBuffer.Trim().Length > 0)
        {
            outfit.Name = renameBuffer.Trim();
            config.Save();
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
        {
            config.Current.Outfits.Remove(outfit);
            config.Save();
            selectedId = null;
            return;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Strings.Get("Outfit_Delete", "Delete outfit"));
        }

        DrawFillButtons(outfit);

        var statuses = OutfitPlanner.Evaluate(outfit, ownership.Locate);
        var missing = 0;
        foreach (var status in statuses)
        {
            if (status.IsMissing)
            {
                missing++;
            }
        }

        if (statuses.Count == 0)
        {
            ImGui.TextDisabled(Strings.Get("Outfit_Empty", "Nothing planned yet. Pick items per slot below, or fill from your gear or a glamour plate."));
        }
        else if (missing == 0)
        {
            ImGui.TextColored(Theme.OwnedColor, Strings.Get("Outfit_Complete", "You have every item."));
        }
        else
        {
            ImGui.TextColored(Theme.MissingColor, string.Format(Strings.Get("Outfit_MissingCount", "{0} of {1} items missing"), missing, statuses.Count));
        }

        DrawSlotTable(outfit);
        DrawSourcesFooter();
        DrawPicker(outfit);
        DrawDyePicker(outfit);
    }

    private void DrawFillButtons(Outfit outfit)
    {
        if (ImGui.Button(Strings.Get("Outfit_FillGear", "Fill from current gear")))
        {
            outfit.Fill(EquippedGearReader.Read(), null);
            config.Save();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Strings.Get("Outfit_FillGearHint", "Replaces every slot with what you are wearing now."));
        }

        ImGui.SameLine();
        var platesLoaded = GlamourPlateReader.IsLoaded;
        using (ImRaii.Disabled(!platesLoaded))
        {
            ImGui.SetNextItemWidth(170 * ImGuiHelpers.GlobalScale);
            using var combo = ImRaii.Combo("##plate", Strings.Get("Outfit_FillPlate", "Fill from glamour plate"));
            if (combo.Success)
            {
                foreach (var plate in GlamourPlateReader.Read())
                {
                    var label = plate.IsEmpty
                        ? string.Format(Strings.Get("Outfit_PlateEmpty", "Plate {0} (empty)"), plate.Number)
                        : string.Format(Strings.Get("Outfit_PlateLabel", "Plate {0}: {1}"), plate.Number, FirstItemName(plate));
                    using var disabled = ImRaii.Disabled(plate.IsEmpty);
                    if (ImGui.Selectable(label))
                    {
                        outfit.Fill(plate.Items, plate.Dyes);
                        config.Save();
                    }
                }
            }
        }

        if (!platesLoaded && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(Strings.Get("Outfit_PlatesNotLoaded", "Open the Glamour Dresser once this session to load your plates."));
        }
    }

    private string FirstItemName(PlateContents plate)
    {
        foreach (var slot in OutfitPlanner.SlotOrder)
        {
            if (plate.Items.TryGetValue(slot, out var id) && catalog.Find(id) is { } item)
            {
                return item.Name;
            }
        }

        return "?";
    }

    private void DrawSlotTable(Outfit outfit)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var iconSize = new Vector2(24, 24) * scale;

        using var table = ImRaii.Table("##slots", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp);
        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupColumn(Strings.Get("Outfit_Slot", "Slot"), ImGuiTableColumnFlags.WidthFixed, 80 * scale);
        ImGui.TableSetupColumn(Strings.Get("Outfit_Item", "Item"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(Strings.Get("Outfit_Dyes", "Dyes"), ImGuiTableColumnFlags.WidthFixed, 56 * scale);
        ImGui.TableSetupColumn(Strings.Get("Outfit_Where", "Where"), ImGuiTableColumnFlags.WidthFixed, 110 * scale);
        ImGui.TableSetupColumn("##actions", ImGuiTableColumnFlags.WidthFixed, 60 * scale);
        ImGui.TableHeadersRow();

        foreach (var slot in OutfitPlanner.SlotOrder)
        {
            using var id = ImRaii.PushId((int)slot);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(GearLabels.For(slot));

            ImGui.TableNextColumn();
            outfit.Items.TryGetValue(slot, out var itemId);
            var item = itemId == 0 ? null : catalog.Find(itemId);
            if (item is null)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled(itemId == 0 ? "-" : string.Format(Strings.Get("Outfit_UnknownItem", "Unknown item {0}"), itemId));
            }
            else
            {
                DrawIcon(item.IconId, iconSize);
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(item.Name);
            }

            ImGui.TableNextColumn();
            if (itemId != 0)
            {
                DrawDyeSwatches(outfit, slot);
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (itemId != 0)
            {
                DrawWhere(outfit, slot, itemId);
            }

            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Search))
            {
                pickingSlot = slot;
                pickerSearch = string.Empty;
                ImGui.OpenPopup("##picker");
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Strings.Get("Outfit_Pick", "Pick an item"));
            }

            if (itemId != 0)
            {
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Times))
                {
                    outfit.Items.Remove(slot);
                    outfit.Dyes.Remove(slot);
                    config.Save();
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Get("Outfit_Clear", "Clear slot"));
                }
            }
        }
    }

    private void DrawWhere(Outfit outfit, GearSlot slot, uint itemId)
    {
        var where = ownership.Locate(itemId);
        if (where == ItemOwnership.Missing)
        {
            ImGui.TextColored(Theme.MissingColor, GearLabels.For(where));
            return;
        }

        ImGui.TextColored(Theme.OwnedColor, GearLabels.For(where));
        var dye = outfit.DyeFor(slot);
        if (where == ItemOwnership.GlamourDresser && !dye.IsUndyed)
        {
            ImGui.SameLine();
            if (dresser.HasDye(itemId, dye))
            {
                ImGui.TextDisabled(Strings.Get("Outfit_DyeMatch", "(this dye)"));
            }
            else
            {
                ImGui.TextDisabled(Strings.Get("Outfit_DyeOther", "(other dye)"));
            }
        }
    }

    private void DrawDyeSwatches(Outfit outfit, GearSlot slot)
    {
        var dye = outfit.DyeFor(slot);
        DrawSwatch(slot, false, dye.Primary);
        ImGui.SameLine();
        DrawSwatch(slot, true, dye.Secondary);
    }

    private void DrawSwatch(GearSlot slot, bool secondary, uint stainId)
    {
        var size = new Vector2(20, 20) * ImGuiHelpers.GlobalScale;
        var stain = stainId == 0 ? null : stains.Find(stainId);
        var color = stain?.Color ?? new Vector4(0, 0, 0, 0);
        using var id = ImRaii.PushId(secondary ? "dye2" : "dye1");
        if (ImGui.ColorButton("##swatch", color, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.AlphaPreview, size))
        {
            pickingDye = (slot, secondary);
            dyeSearch = string.Empty;
            ImGui.OpenPopup("##dyePicker");
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(stain?.Name ?? Strings.Get("Dye_None", "No dye"));
        }

        DrawDyePickerPopup(slot, secondary);
    }

    private void DrawDyePickerPopup(GearSlot slot, bool secondary)
    {
        if (pickingDye != (slot, secondary))
        {
            return;
        }

        var outfit = Selected();
        if (outfit is null)
        {
            pickingDye = null;
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowSize(new Vector2(280, 400) * scale, ImGuiCond.Appearing);
        using var popup = ImRaii.Popup("##dyePicker");
        if (!popup.Success)
        {
            pickingDye = null;
            return;
        }

        ImGui.SetNextItemWidth(-1);
        if (ImGui.IsWindowAppearing())
        {
            ImGui.SetKeyboardFocusHere();
        }

        ImGui.InputTextWithHint("##dyeSearch", Strings.Get("Search_Hint", "Search"), ref dyeSearch, 40);

        using var list = ImRaii.Child("##dyeList", new Vector2(-1, -1), true);
        if (!list.Success)
        {
            return;
        }

        if (ImGui.Selectable(Strings.Get("Dye_None", "No dye")))
        {
            ApplyDye(outfit, slot, secondary, 0);
            ImGui.CloseCurrentPopup();
        }

        var swatch = new Vector2(16, 16) * scale;
        foreach (var stain in stains.Search(dyeSearch))
        {
            using var id = ImRaii.PushId((int)stain.Id);
            ImGui.ColorButton("##c", stain.Color, ImGuiColorEditFlags.NoTooltip, swatch);
            ImGui.SameLine();
            if (ImGui.Selectable(stain.Name))
            {
                ApplyDye(outfit, slot, secondary, stain.Id);
                ImGui.CloseCurrentPopup();
            }
        }
    }

    private void ApplyDye(Outfit outfit, GearSlot slot, bool secondary, uint stainId)
    {
        var current = outfit.DyeFor(slot);
        var updated = secondary ? new DyePair(current.Primary, stainId) : new DyePair(stainId, current.Secondary);
        outfit.SetDye(slot, updated);
        config.Save();
    }

    private void DrawPicker(Outfit outfit)
    {
        if (pickingSlot is not { } slot)
        {
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetNextWindowSize(new Vector2(420, 480) * scale, ImGuiCond.Appearing);
        using var popup = ImRaii.Popup("##picker");
        if (!popup.Success)
        {
            pickingSlot = null;
            return;
        }

        ImGui.Text(string.Format(Strings.Get("Outfit_PickFor", "Pick for {0}"), GearLabels.For(slot)));
        ImGui.SetNextItemWidth(-1);
        if (ImGui.IsWindowAppearing())
        {
            ImGui.SetKeyboardFocusHere();
        }

        ImGui.InputTextWithHint("##pickerSearch", Strings.Get("Search_Hint", "Search"), ref pickerSearch, 60);

        using var list = ImRaii.Child("##pickerList", new Vector2(-1, -1), true);
        if (!list.Success)
        {
            return;
        }

        var iconSize = new Vector2(24, 24) * scale;
        var shown = 0;
        foreach (var item in catalog.Search(slot, pickerSearch, PickerLimit))
        {
            shown++;
            using var id = ImRaii.PushId((int)item.ItemId);
            DrawIcon(item.IconId, iconSize);
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            if (ImGui.Selectable(item.Name))
            {
                outfit.Items[slot] = item.ItemId;
                config.Save();
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(string.Format(Strings.Get("Outfit_ItemTooltip", "Item {0}, level {1}, {2}"), item.ItemId, item.EquipLevel, GearLabels.For(ownership.Locate(item.ItemId))));
            }
        }

        if (shown == 0)
        {
            ImGui.TextDisabled(Strings.Get("Table_Empty", "Nothing matches."));
        }
        else if (shown >= PickerLimit)
        {
            ImGui.TextDisabled(Strings.Get("Outfit_PickerTruncated", "Showing the newest matches only; type more to narrow down."));
        }
    }

    private void DrawDyePicker(Outfit outfit)
    {
        // Popups are drawn next to their swatch in DrawSwatch; nothing to do here, kept for symmetry with DrawPicker.
        _ = outfit;
    }

    private void DrawSourcesFooter()
    {
        ImGui.Spacing();
        var dresserState = dresser.SnapshotTakenAt is { } at
            ? string.Format(Strings.Get("Dresser_Snapshot", "Glamour Dresser: {0} items as of {1:g}{2}"), dresser.SnapshotCount, at.ToLocalTime(), GlamourDresserReader.IsLoaded ? string.Empty : Strings.Get("Dresser_Stale", " (open the dresser to refresh)"))
            : Strings.Get("Dresser_Never", "Glamour Dresser: not read yet; open it once so its contents count as owned.");
        ImGui.TextDisabled(dresserState);
        if (!ArmoireModule.IsLoaded)
        {
            ImGui.TextDisabled(Strings.Get("Armoire_NotLoadedShort", "Armoire: not loaded this session; open it once at an inn."));
        }
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
