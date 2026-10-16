using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using NekoDex.Core.Collections;
using NekoDex.Core.Unlock;

namespace NekoDex.UI;

/// <summary>Draws one collection module: progress, filters, a sortable entry table with pins, and a detail pane for the selected entry.</summary>
public sealed unsafe class CollectionTab
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly ITextureProvider textureProvider;
    private readonly UnlockResolver unlockResolver;
    private readonly DatasetUnlockSourceProvider? dataset;
    private readonly PinController pins;
    private readonly IDetailExtension? detailExtension;
    private readonly IReadOnlyList<EntryFilterOption> moduleFilters;
    private readonly HashSet<string> activeModuleFilters = new();
    private bool hasCategory;
    private bool hasRank;

    private CollectionSnapshot snapshot = CollectionSnapshot.Empty;
    private IReadOnlyList<EntryView> views = [];
    private IReadOnlyList<EntryView> visible = [];
    private DateTime lastRefresh = DateTime.MinValue;
    private bool dirty = true;

    private string search = string.Empty;
    private OwnershipFilter ownership = OwnershipFilter.All;
    private AvailabilityFilter availability = AvailabilityFilter.All;
    private EntrySortKey sortKey = EntrySortKey.Default;
    private bool sortDescending;
    private bool missingFirst;
    private bool latestOnly;
    private uint? selectedId;

    public CollectionTab(ICollectionModule module, ITextureProvider textureProvider, UnlockResolver unlockResolver, DatasetUnlockSourceProvider? dataset, PinController pins, IDetailExtension? detailExtension = null)
    {
        Module = module;
        this.textureProvider = textureProvider;
        this.unlockResolver = unlockResolver;
        this.dataset = dataset;
        this.pins = pins;
        this.detailExtension = detailExtension;
        moduleFilters = module is IFilteredModule filtered ? filtered.Filters : [];
    }

    public ICollectionModule Module { get; }

    /// <summary>Newest patch in the unlock data for this module, or null when unknown.</summary>
    public string? LatestPatch => dataset?.LatestPatch(Module.Id);

    /// <summary>Turns on the "new in latest patch" filter, e.g. when the Dashboard sends the user here.</summary>
    public void ShowLatestOnly()
    {
        latestOnly = LatestPatch is not null;
        dirty = true;
    }

    /// <summary>How many entries were added in the latest patch and how many of those are still missing. Only call while a character is loaded.</summary>
    public (string Patch, int Total, int Missing)? NewThisPatch()
    {
        if (LatestPatch is not { } patch)
        {
            return null;
        }

        RefreshIfDue();
        var total = 0;
        var missing = 0;
        foreach (var view in views)
        {
            if (!EntryViewQuery.IsPatch(view.Source.Patch, patch))
            {
                continue;
            }

            total++;
            if (!view.Owned)
            {
                missing++;
            }
        }

        return (patch, total, missing);
    }

    /// <summary>Forget the current snapshot, e.g. after logout, so stale ownership is never shown for another character.</summary>
    public void Reset()
    {
        snapshot = CollectionSnapshot.Empty;
        views = [];
        visible = [];
        lastRefresh = DateTime.MinValue;
        selectedId = null;
        dirty = true;
    }

    /// <summary>Latest snapshot, refreshed if due. Only call while a character is loaded.</summary>
    public CollectionSnapshot CurrentSnapshot()
    {
        RefreshIfDue();
        return snapshot;
    }

    /// <summary>Every entry with ownership and unlock source, module order, refreshed if due. Only call while a character is loaded.</summary>
    public IReadOnlyList<EntryView> Views
    {
        get
        {
            RefreshIfDue();
            return views;
        }
    }

    /// <summary>Pinned entries with their ownership, module order. Only call while a character is loaded.</summary>
    public IEnumerable<EntryView> PinnedViews()
    {
        RefreshIfDue();
        foreach (var view in views)
        {
            if (view.Pinned)
            {
                yield return view;
            }
        }
    }

    public void Draw()
    {
        RefreshIfDue();

        if (Module.UnavailableReason is { } reason)
        {
            ImGui.TextColored(Theme.MissingColor, reason);
        }

        DrawProgress();
        DrawControls();

        if (dirty)
        {
            var onlyPatch = latestOnly ? LatestPatch : null;
            Func<CollectionEntry, bool>? extra = null;
            foreach (var option in moduleFilters)
            {
                if (activeModuleFilters.Contains(option.Id))
                {
                    var previous = extra;
                    extra = previous is null ? option.Predicate : e => previous(e) && option.Predicate(e);
                }
            }

            visible = EntryViewQuery.Sort(EntryViewQuery.Filter(views, ownership, availability, search, onlyPatch, extra), sortKey, sortDescending, missingFirst, pinnedFirst: true);
            dirty = false;
        }

        var detailHeight = (detailExtension is null ? 150 : 210) * ImGuiHelpers.GlobalScale;
        DrawTable(detailHeight);
        DrawDetail(detailHeight);
    }

    private void RefreshIfDue()
    {
        var now = DateTime.UtcNow;
        if (now - lastRefresh < RefreshInterval)
        {
            return;
        }

        snapshot = CollectionSnapshot.Build(Module);
        RebuildViews();
        lastRefresh = now;
    }

    private void RebuildViews()
    {
        var list = new EntryView[snapshot.Entries.Count];
        for (var i = 0; i < list.Length; i++)
        {
            var tracked = snapshot.Entries[i];
            list[i] = new EntryView(tracked, unlockResolver.Resolve(Module.Id, tracked.Entry), pins.IsPinned(Module.Id, tracked.Entry.Id));
        }

        views = list;
        hasCategory = list.Any(v => v.Entry.Category is not null);
        hasRank = list.Any(v => v.Entry.Rank > 0);
        dirty = true;
    }

    private void DrawProgress()
    {
        var progress = snapshot.Progress;
        var overlay = string.Format(Strings.Get("Progress_Overlay", "{0} / {1}"), progress.Owned, progress.Total);
        ImGui.ProgressBar(progress.Fraction, new Vector2(-1, 0), overlay);
    }

    private void DrawControls()
    {
        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputTextWithHint("##search", Strings.Get("Search_Hint", "Search"), ref search, 100))
        {
            dirty = true;
        }

        ImGui.SameLine();
        Radio(ref ownership, OwnershipFilter.All, Strings.Get("Filter_All", "All"));
        ImGui.SameLine();
        Radio(ref ownership, OwnershipFilter.Owned, Strings.Get("Filter_Owned", "Owned"));
        ImGui.SameLine();
        Radio(ref ownership, OwnershipFilter.Missing, Strings.Get("Filter_Missing", "Missing"));

        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();
        if (ImGui.Checkbox(Strings.Get("Sort_MissingFirst", "Missing first"), ref missingFirst))
        {
            dirty = true;
        }

        Radio(ref availability, AvailabilityFilter.All, Strings.Get("Avail_All", "Any source"));
        ImGui.SameLine();
        Radio(ref availability, AvailabilityFilter.Obtainable, Strings.Get("Avail_Obtainable", "Obtainable"));
        ImGui.SameLine();
        Radio(ref availability, AvailabilityFilter.NotObtainable, Strings.Get("Avail_NotObtainable", "Not obtainable"));
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(Strings.Get("Avail_Help", "Not obtainable: store-only, past events and legacy rewards according to the unlock data. Entries without data count as obtainable."));

        if (LatestPatch is { } patch)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("|");
            ImGui.SameLine();
            if (ImGui.Checkbox(string.Format(Strings.Get("Filter_NewInPatch", "New in {0}"), patch), ref latestOnly))
            {
                dirty = true;
            }
        }

        foreach (var option in moduleFilters)
        {
            var on = activeModuleFilters.Contains(option.Id);
            if (ImGui.Checkbox(option.Label, ref on))
            {
                if (on)
                {
                    activeModuleFilters.Add(option.Id);
                }
                else
                {
                    activeModuleFilters.Remove(option.Id);
                }

                dirty = true;
            }
        }
    }

    private void Radio<T>(ref T field, T value, string label) where T : struct, Enum
    {
        if (ImGui.RadioButton(label, field.Equals(value)))
        {
            field = value;
            dirty = true;
        }
    }

    private void DrawTable(float reservedBelow)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var iconSize = new Vector2(28, 28) * scale;
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Sortable | ImGuiTableFlags.SortTristate;
        var spacing = ImGui.GetStyle().ItemSpacing.Y;

        var columns = 6 + (hasRank ? 1 : 0) + (hasCategory ? 1 : 0);
        using var table = ImRaii.Table($"##{Module.Id}", columns, flags, new Vector2(-1, -(reservedBelow + spacing)));
        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("##pin", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, 24 * scale, 0);
        ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort, iconSize.X + 4, 0);
        ImGui.TableSetupColumn(Strings.Get("Column_Name", "Name"), ImGuiTableColumnFlags.WidthStretch, 0, (uint)EntrySortKey.Name);
        if (hasRank)
        {
            ImGui.TableSetupColumn(Strings.Get("Column_Rank", "Stars"), ImGuiTableColumnFlags.WidthFixed, 44 * scale, (uint)EntrySortKey.Rank);
        }

        if (hasCategory)
        {
            ImGui.TableSetupColumn(Strings.Get("Column_Category", "Type"), ImGuiTableColumnFlags.WidthFixed, 70 * scale, (uint)EntrySortKey.Category);
        }

        ImGui.TableSetupColumn(Strings.Get("Column_Patch", "Patch"), ImGuiTableColumnFlags.WidthFixed, 46 * scale, (uint)EntrySortKey.Patch);
        ImGui.TableSetupColumn(Strings.Get("Column_Status", "Status"), ImGuiTableColumnFlags.WidthFixed, 70 * scale, (uint)EntrySortKey.Status);
        ImGui.TableSetupColumn(Strings.Get("Column_Unlock", "Unlock"), ImGuiTableColumnFlags.WidthFixed, 130 * scale, (uint)EntrySortKey.UnlockType);
        ImGui.TableHeadersRow();
        ReadSortSpecs();

        var ownedLabel = Strings.Get("Status_Owned", "Owned");
        var missingLabel = Strings.Get("Status_Missing", "Missing");

        foreach (var view in visible)
        {
            var entry = view.Entry;
            using var id = ImRaii.PushId((int)entry.Id);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            DrawPinButton(view);

            ImGui.TableNextColumn();
            DrawIcon(entry.IconId, iconSize);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (ImGui.Selectable(entry.Name, selectedId == entry.Id, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap))
            {
                selectedId = entry.Id;
            }

            if (hasRank)
            {
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled(entry.Rank > 0 ? new string('\u2605', entry.Rank) : string.Empty);
            }

            if (hasCategory)
            {
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled(entry.Category ?? string.Empty);
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled(view.Source.Patch ?? string.Empty);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (view.Owned)
            {
                ImGui.TextColored(Theme.OwnedColor, ownedLabel);
            }
            else
            {
                ImGui.TextColored(Theme.MissingColor, missingLabel);
            }

            ImGui.TableNextColumn();
            DrawUnlockCell(view.Source);
        }

        if (visible.Count == 0)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TextDisabled(Strings.Get("Table_Empty", "Nothing matches."));
        }
    }

    private void ReadSortSpecs()
    {
        var specs = ImGui.TableGetSortSpecs();
        if (specs.IsNull || !specs.SpecsDirty)
        {
            return;
        }

        if (specs.SpecsCount > 0 && !specs.Specs.IsNull)
        {
            sortKey = (EntrySortKey)specs.Specs.ColumnUserID;
            sortDescending = specs.Specs.SortDirection == ImGuiSortDirection.Descending;
        }
        else
        {
            sortKey = EntrySortKey.Default;
            sortDescending = false;
        }

        specs.SpecsDirty = false;
        dirty = true;
    }

    private void DrawPinButton(EntryView view)
    {
        var icon = view.Pinned ? FontAwesomeIcon.Star : FontAwesomeIcon.StarHalfAlt;
        using (ImRaii.PushColor(ImGuiCol.Text, view.Pinned ? Theme.PinColor : ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]))
        using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0)))
        {
            if (ImGuiComponents.IconButton(icon))
            {
                pins.Toggle(Module.Id, view.Entry.Id);
                RebuildViews();
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(view.Pinned ? Strings.Get("Pin_Remove", "Unpin") : Strings.Get("Pin_Add", "Pin: show first here and in the mini window"));
        }
    }

    /// <summary>Type label with the summary as tooltip, plus a button that opens the web page when one exists.</summary>
    private void DrawUnlockCell(UnlockSource source)
    {
        if (source.Url is not null)
        {
            DrawOpenLinkButton(source.Url);
            ImGui.SameLine();
        }

        ImGui.AlignTextToFramePadding();
        if (source.IsUnknown)
        {
            ImGui.TextDisabled(UnlockLabels.For(source.Type));
        }
        else
        {
            ImGui.Text(UnlockLabels.For(source.Type));
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(source.IsUnknown
                ? source.Note ?? Strings.Get("Unlock_UnknownHint", "No unlock information yet.")
                : source.Summary);
        }
    }

    private void DrawDetail(float height)
    {
        using var child = ImRaii.Child("##detail", new Vector2(-1, height), true);
        if (!child.Success)
        {
            return;
        }

        var selected = FindSelected();
        if (selected is null)
        {
            ImGui.TextDisabled(Strings.Get("Detail_Hint", "Select an entry to see how to get it."));
            DrawDataFooter();
            return;
        }

        var entry = selected.Entry;
        var source = selected.Source;
        var scale = ImGuiHelpers.GlobalScale;

        DrawIcon(entry.IconId, new Vector2(40, 40) * scale);
        ImGui.SameLine();
        using (ImRaii.Group())
        {
            ImGui.Text(entry.Name);
            ImGui.SameLine();
            if (selected.Owned)
            {
                ImGui.TextColored(Theme.OwnedColor, Strings.Get("Status_Owned", "Owned"));
            }
            else
            {
                ImGui.TextColored(Theme.MissingColor, Strings.Get("Status_Missing", "Missing"));
            }

            if (source.Patch is not null)
            {
                ImGui.SameLine();
                if (LatestPatch is { } latest && EntryViewQuery.IsPatch(source.Patch, latest))
                {
                    ImGui.TextColored(Theme.PinColor, string.Format(Strings.Get("Detail_PatchNew", "New in patch {0}"), source.Patch));
                }
                else
                {
                    ImGui.TextDisabled(string.Format(Strings.Get("Detail_Patch", "Patch {0}"), source.Patch));
                }
            }

            if (!UnlockAvailability.IsObtainable(source))
            {
                ImGui.SameLine();
                ImGui.TextColored(Theme.MissingColor, Strings.Get("Detail_NotObtainable", "Not obtainable"));
            }

            ImGui.Text(UnlockLabels.For(source.Type));
            if (source.Url is not null)
            {
                ImGui.SameLine();
                DrawOpenLinkButton(source.Url);
            }
        }

        ImGui.Spacing();
        if (source.IsUnknown)
        {
            ImGui.TextWrapped(source.Note ?? Strings.Get("Unlock_UnknownHint", "No unlock information yet."));
            if (source.Url is not null)
            {
                ImGui.TextWrapped(Strings.Get("Detail_UseWeb", "Use the web link above to look it up."));
            }
        }
        else
        {
            foreach (var line in source.DetailLines)
            {
                ImGui.Bullet();
                ImGui.SameLine();
                ImGui.TextWrapped(line);
            }

            if (source.Note is not null)
            {
                ImGui.TextDisabled(source.Note);
            }
        }

        detailExtension?.Draw(entry);
    }

    private void DrawDataFooter()
    {
        if (dataset is null)
        {
            ImGui.TextDisabled(Strings.Get("Detail_NoDataset", "Bundled unlock data unavailable; web links only."));
            return;
        }

        ImGui.TextDisabled(string.Format(
            Strings.Get("Detail_DataFooter", "Unlock data {0} from {1}, generated for game {2}."),
            dataset.DataVersion,
            dataset.Source,
            dataset.GameVersion));
    }

    private EntryView? FindSelected()
    {
        if (selectedId is not { } id)
        {
            return null;
        }

        foreach (var view in views)
        {
            if (view.Entry.Id == id)
            {
                return view;
            }
        }

        return null;
    }

    private static void DrawOpenLinkButton(string url)
    {
        if (ImGuiComponents.IconButton(FontAwesomeIcon.ExternalLinkAlt))
        {
            Util.OpenLink(url);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(string.Format(Strings.Get("Tooltip_OpenOnWeb", "Open on {0}"), HostOf(url)));
        }
    }

    private static string HostOf(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;

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
