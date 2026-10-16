using NekoDex.Core.Outfits;

namespace NekoDex.UI;

internal static class GearLabels
{
    public static string For(GearSlot slot) => slot switch
    {
        GearSlot.MainHand => Strings.Get("Slot_MainHand", "Main hand"),
        GearSlot.OffHand => Strings.Get("Slot_OffHand", "Off hand"),
        GearSlot.Head => Strings.Get("Slot_Head", "Head"),
        GearSlot.Body => Strings.Get("Slot_Body", "Body"),
        GearSlot.Hands => Strings.Get("Slot_Hands", "Hands"),
        GearSlot.Legs => Strings.Get("Slot_Legs", "Legs"),
        GearSlot.Feet => Strings.Get("Slot_Feet", "Feet"),
        GearSlot.Ears => Strings.Get("Slot_Ears", "Ears"),
        GearSlot.Neck => Strings.Get("Slot_Neck", "Neck"),
        GearSlot.Wrists => Strings.Get("Slot_Wrists", "Wrists"),
        GearSlot.RingLeft => Strings.Get("Slot_RingLeft", "Ring (L)"),
        GearSlot.RingRight => Strings.Get("Slot_RingRight", "Ring (R)"),
        _ => slot.ToString(),
    };

    public static string For(ItemOwnership ownership) => ownership switch
    {
        ItemOwnership.Inventory => Strings.Get("Own_Inventory", "Bags"),
        ItemOwnership.Armoire => Strings.Get("Own_Armoire", "Armoire"),
        ItemOwnership.GlamourDresser => Strings.Get("Own_Dresser", "Dresser"),
        _ => Strings.Get("Status_Missing", "Missing"),
    };
}
