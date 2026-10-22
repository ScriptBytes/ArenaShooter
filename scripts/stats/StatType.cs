namespace ArenaShooter;

/// <summary>
/// Player stats that items (and later classes, spells, potions) can modify.
/// Values are stored by number in .tres files, so never renumber existing entries.
/// </summary>
public enum StatType
{
    MaxHealth = 0,
    Armor = 1,
    Damage = 2,       // percent
    AttackSpeed = 3,  // percent
    MoveSpeed = 4,    // percent
    Range = 5,
    PickupRange = 6,
    CritChance = 7,   // percent, added to each ability's own base chance
}

public static class StatInfo
{
    public static string Label(StatType stat) => stat switch
    {
        StatType.MaxHealth => "Max HP",
        StatType.Armor => "Armor",
        StatType.Damage => "Damage",
        StatType.AttackSpeed => "Attack Speed",
        StatType.MoveSpeed => "Move Speed",
        StatType.Range => "Range",
        StatType.PickupRange => "Pickup Range",
        StatType.CritChance => "Crit Chance",
        _ => stat.ToString(),
    };

    public static bool IsPercent(StatType stat) =>
        stat is StatType.Damage or StatType.AttackSpeed or StatType.MoveSpeed or StatType.CritChance;

    /// <summary>Formats a bonus like "+15% Damage" or "-3 Max HP".</summary>
    public static string Format(StatType stat, float value)
    {
        string sign = value >= 0 ? "+" : "";
        string unit = IsPercent(stat) ? "%" : "";
        return $"{sign}{value:0.#}{unit} {Label(stat)}";
    }
}
