namespace ArenaShooter;

/// <summary>
/// Where a piece of gear is worn. <see cref="None"/> means a trinket: it has no slot and
/// copies stack. Values are stored by number in .tres files, so never renumber existing entries.
/// </summary>
public enum EquipSlot
{
    None = 0,
    MainHand = 1,
    OffHand = 2,
    Armor = 3,
    Ring = 4,
    Amulet = 5,
}

public static class EquipSlotInfo
{
    public static string Label(EquipSlot slot) => slot switch
    {
        EquipSlot.None => "Trinket",
        EquipSlot.MainHand => "Main Hand",
        EquipSlot.OffHand => "Off Hand",
        _ => slot.ToString(),
    };
}
