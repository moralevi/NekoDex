using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using NekoDex.Core.Collections;
using NekoDex.Data;

namespace NekoDex.UI;

/// <summary>For a card: the NPCs that can drop it, where they stand, whether you can challenge them yet, and a button that flags them on the map.</summary>
public sealed class TriadDetailExtension : IDetailExtension
{
    private readonly TriadNpcIndex index;
    private readonly IGameGui gameGui;

    public TriadDetailExtension(TriadNpcIndex index, IGameGui gameGui)
    {
        this.index = index;
        this.gameGui = gameGui;
    }

    public void Draw(CollectionEntry entry)
    {
        var npcs = index.NpcsFor(entry.Id);
        if (npcs.Count == 0)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.TextDisabled(Strings.Get("Triad_NpcHeader", "Won from"));
        foreach (var npc in npcs)
        {
            using var id = ImRaii.PushId((int)npc.NpcId);
            var unlocked = TriadNpcIndex.IsUnlocked(npc);

            if (npc.HasPosition)
            {
                if (ImGuiComponents.IconButton(FontAwesomeIcon.MapMarkerAlt))
                {
                    gameGui.OpenMapWithMapLink(npc.TerritoryId, npc.MapId, npc.Position!.Value);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Strings.Get("Triad_Flag", "Open the map with a flag on this NPC"));
                }

                ImGui.SameLine();
            }

            ImGui.AlignTextToFramePadding();
            var label = npc.Place.Length > 0 ? $"{npc.Name}, {npc.Place}" : npc.Name;
            if (unlocked)
            {
                ImGui.Text(label);
            }
            else
            {
                ImGui.TextDisabled(label);
                ImGui.SameLine();
                ImGui.TextColored(Theme.MissingColor, string.Format(
                    Strings.Get("Triad_Locked", "(needs {0})"),
                    string.Join(npc.NeedsAllQuests ? " + " : " / ", npc.UnlockQuests.Select(q => q.QuestName))));
            }
        }
    }
}
