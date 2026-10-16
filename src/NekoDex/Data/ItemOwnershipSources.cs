using FFXIVClientStructs.FFXIV.Client.Game;
using NekoDex.Core.Outfits;

namespace NekoDex.Data;

/// <summary>Where an item is, for the outfit planner: Glamour Dresser snapshot, Armoire (when loaded), then bags, armoury chest and equipped gear.</summary>
public sealed unsafe class ItemOwnershipSources
{
    private readonly GlamourDresserReader dresser;
    private readonly ArmoireModule armoire;

    public ItemOwnershipSources(GlamourDresserReader dresser, ArmoireModule armoire)
    {
        this.dresser = dresser;
        this.armoire = armoire;
    }

    public ItemOwnership Locate(uint itemId)
    {
        if (dresser.Contains(itemId))
        {
            return ItemOwnership.GlamourDresser;
        }

        if (armoire.IsItemInArmoire(itemId))
        {
            return ItemOwnership.Armoire;
        }

        var inventory = InventoryManager.Instance();
        if (inventory != null
            && (inventory->GetInventoryItemCount(itemId, false, true, true, 0) > 0
                || inventory->GetInventoryItemCount(itemId, true, true, true, 0) > 0))
        {
            return ItemOwnership.Inventory;
        }

        return ItemOwnership.Missing;
    }
}
