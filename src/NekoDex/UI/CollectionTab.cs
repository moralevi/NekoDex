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

/// <summary>Draws one collection module: progress, search, ownership filter and the entry table.</summary>
public sealed class CollectionTab
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly ITextureProvider textureProvider;
    private readonly UnlockResolver unlockResolver;
    private CollectionSnapshot snapshot = CollectionSnapshot.Empty;
    private DateTime lastRefresh = DateTime.MinValue;
    private string search = string.Empty;
    private OwnershipFilter filter = OwnershipFilter.All;

    public CollectionTab(ICollectionModule module, ITextureProvider textureProvider, UnlockResolver unlockResolver)
    {
        Module = module;
        this.textureProvider = textureProvider;
        this.unlockResolver = unlockResolver;
    }

    public ICollectionModule Module { get; }

    /// <summary>Forget the current snapshot, e.g. after logout, so stale ownership is never shown for another character.</summary>
    public void Reset()
    {
        snapshot = CollectionSnapshot.Empty;
        lastRefresh = DateTime.MinValue;
    }

    public void Draw()
    {
        RefreshIfDue();

        DrawProgress();
        DrawControls();
        DrawTable();
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

    private void DrawTable()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var iconSize = new Vector2(28, 28) * scale;
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;

        using var table = ImRaii.Table($"##{Module.Id}", 4, flags, new Vector2(-1, -1));
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
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            DrawIcon(tracked.Entry.IconId, iconSize);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(tracked.Entry.Name);

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
            DrawUnlock(tracked.Entry);
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
    private void DrawUnlock(CollectionEntry entry)
    {
        var source = unlockResolver.Resolve(Module.Id, entry);

        if (source.Url is not null)
        {
            if (ImGuiComponents.IconButton((int)entry.Id, FontAwesomeIcon.ExternalLinkAlt))
            {
                Util.OpenLink(source.Url);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(string.Format(Strings.Get("Tooltip_OpenOnWeb", "Open on {0}"), HostOf(source.Url)));
            }

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
                ? Strings.Get("Unlock_UnknownHint", "No unlock information yet.")
                : source.Summary);
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
