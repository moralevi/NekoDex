using System;
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

/// <summary>Draws one collection module: progress, search, ownership filter, the entry table and a detail pane for the selected entry.</summary>
public sealed class CollectionTab
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly ITextureProvider textureProvider;
    private readonly UnlockResolver unlockResolver;
    private readonly DatasetUnlockSourceProvider? dataset;
    private CollectionSnapshot snapshot = CollectionSnapshot.Empty;
    private DateTime lastRefresh = DateTime.MinValue;
    private string search = string.Empty;
    private OwnershipFilter filter = OwnershipFilter.All;
    private uint? selectedId;

    public CollectionTab(ICollectionModule module, ITextureProvider textureProvider, UnlockResolver unlockResolver, DatasetUnlockSourceProvider? dataset)
    {
        Module = module;
        this.textureProvider = textureProvider;
        this.unlockResolver = unlockResolver;
        this.dataset = dataset;
    }

    public ICollectionModule Module { get; }

    /// <summary>Forget the current snapshot, e.g. after logout, so stale ownership is never shown for another character.</summary>
    public void Reset()
    {
        snapshot = CollectionSnapshot.Empty;
        lastRefresh = DateTime.MinValue;
        selectedId = null;
    }

    public void Draw()
    {
        RefreshIfDue();

        DrawProgress();
        DrawControls();

        var detailHeight = 150 * ImGuiHelpers.GlobalScale;
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
        lastRefresh = now;
    }

    private void DrawProgress()
    {
        var progress = snapshot.Progress;
        var overlay = string.Format(Strings.Get("Progress_Overlay", "{0} / {1}"), progress.Owned, progress.Total);
        ImGui.ProgressBar(progress.Fraction, new Vector2(-1, 0), overlay);
    }

    private void DrawControls()
    {
        ImGui.SetNextItemWidth(220 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##search", Strings.Get("Search_Hint", "Search"), ref search, 100);

        ImGui.SameLine();
        DrawFilterButton(OwnershipFilter.All, Strings.Get("Filter_All", "All"));
        ImGui.SameLine();
        DrawFilterButton(OwnershipFilter.Owned, Strings.Get("Filter_Owned", "Owned"));
        ImGui.SameLine();
        DrawFilterButton(OwnershipFilter.Missing, Strings.Get("Filter_Missing", "Missing"));
    }

    private void DrawFilterButton(OwnershipFilter value, string label)
    {
        if (ImGui.RadioButton(label, filter == value))
        {
            filter = value;
        }
    }

    private void DrawTable(float reservedBelow)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var iconSize = new Vector2(28, 28) * scale;
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;
        var spacing = ImGui.GetStyle().ItemSpacing.Y;

        using var table = ImRaii.Table($"##{Module.Id}", 4, flags, new Vector2(-1, -(reservedBelow + spacing)));
        if (!table.Success)
        {
            return;
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed, iconSize.X + 4);
        ImGui.TableSetupColumn(Strings.Get("Column_Name", "Name"), ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn(Strings.Get("Column_Status", "Status"), ImGuiTableColumnFlags.WidthFixed, 70 * scale);
        ImGui.TableSetupColumn(Strings.Get("Column_Unlock", "Unlock"), ImGuiTableColumnFlags.WidthFixed, 130 * scale);
        ImGui.TableHeadersRow();

        var ownedLabel = Strings.Get("Status_Owned", "Owned");
        var missingLabel = Strings.Get("Status_Missing", "Missing");
        var any = false;

        foreach (var tracked in EntryQuery.Apply(snapshot.Entries, filter, search))
        {
            any = true;
            var entry = tracked.Entry;
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            DrawIcon(entry.IconId, iconSize);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (ImGui.Selectable($"{entry.Name}##{entry.Id}", selectedId == entry.Id, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap))
            {
                selectedId = entry.Id;
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (tracked.Owned)
            {
                ImGui.TextColored(Theme.OwnedColor, ownedLabel);
            }
            else
            {
                ImGui.TextColored(Theme.MissingColor, missingLabel);
            }

            ImGui.TableNextColumn();
            DrawUnlockCell(entry);
        }

        if (!any)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TextDisabled(Strings.Get("Table_Empty", "Nothing matches."));
        }
    }

    /// <summary>Type label with the summary as tooltip, plus a button that opens the web page when one exists.</summary>
    private void DrawUnlockCell(CollectionEntry entry)
    {
        var source = unlockResolver.Resolve(Module.Id, entry);

        if (source.Url is not null)
        {
            DrawOpenLinkButton((int)entry.Id, source.Url);
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
        var source = unlockResolver.Resolve(Module.Id, entry);
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
                ImGui.TextDisabled(string.Format(Strings.Get("Detail_Patch", "Patch {0}"), source.Patch));
            }

            ImGui.Text(UnlockLabels.For(source.Type));
            if (source.Url is not null)
            {
                ImGui.SameLine();
                DrawOpenLinkButton(-1, source.Url);
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

    private TrackedEntry? FindSelected()
    {
        if (selectedId is not { } id)
        {
            return null;
        }

        foreach (var tracked in snapshot.Entries)
        {
            if (tracked.Entry.Id == id)
            {
                return tracked;
            }
        }

        return null;
    }

    private static void DrawOpenLinkButton(int id, string url)
    {
        if (ImGuiComponents.IconButton(id, FontAwesomeIcon.ExternalLinkAlt))
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

        var wrap = textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrEmpty();
        ImGui.Image(wrap.Handle, size);
    }
}
