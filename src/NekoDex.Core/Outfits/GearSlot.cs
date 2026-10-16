namespace NekoDex.Core.Outfits;

/// <summary>Equipment slots an outfit can fill. Order is display order.</summary>
public enum GearSlot
{
    MainHand = 0,
    OffHand = 1,
    Head = 2,
    Body = 3,
    Hands = 4,
    Legs = 5,
    Feet = 6,
    Ears = 7,
    Neck = 8,
    Wrists = 9,
    RingLeft = 10,
    RingRight = 11,
}

/// <summary>Where an item was found when checking ownership for an outfit.</summary>
public enum ItemOwnership
{
    Missing = 0,
    Inventory = 1,
    Armoire = 2,
    GlamourDresser = 3,
}
