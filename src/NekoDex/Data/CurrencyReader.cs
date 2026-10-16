using FFXIVClientStructs.FFXIV.Client.Game;

namespace NekoDex.Data;

/// <summary>Read-only currency counts for the logged-in character.</summary>
public static unsafe class CurrencyReader
{
    /// <summary>How many of the given tomestone item the character holds; 0 when the game cannot answer.</summary>
    public static int Tomestones(uint tomestoneItemId)
    {
        var manager = InventoryManager.Instance();
        return manager == null ? 0 : (int)manager->GetTomestoneCount(tomestoneItemId);
    }
}
