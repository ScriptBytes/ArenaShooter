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
    LifeSteal = 8,    // percent of damage dealt healed back, added to each ability's own base
    MinionDamage = 9, // percent, stacks additively with Damage for minions only
    PotionHeal = 10,  // flat HP added to the health potion's heal (and the reserve's size)
    CooldownReduction = 11, // percent off spell cooldowns (not the weapon: Attack Speed covers it), capped
    HpRegen = 12,     // HP healed per 10 seconds, over time
    Dodge = 13,       // percent chance to avoid a hit entirely, capped
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
        StatType.LifeSteal => "Life Steal",
        StatType.MinionDamage => "Minion Damage",
        StatType.PotionHeal => "Potion Heal",
        StatType.CooldownReduction => "Cooldown Reduction",
        StatType.HpRegen => "HP Regen",
        StatType.Dodge => "Dodge",
        _ => stat.ToString(),
    };

    /// <summary>Seconds that <see cref="StatType.HpRegen"/> is counted over ("+2 HP Regen per 10s").</summary>
    public const float RegenPeriod = 10f;

    /// <summary>
    /// Highest useful total for a capped stat (Brotato/LoL style), or null when uncapped.
    /// Totals above it are kept (so a debuff can eat into the excess) but have no extra effect.
    /// Crit Chance is capped per ability at 100% in Combat, so it isn't listed here.
    /// </summary>
    public static float? Cap(StatType stat) => stat switch
    {
        StatType.CooldownReduction => 50f,
        StatType.Dodge => 60f,
        _ => null,
    };

    /// <summary>The stat's total with its cap applied (and never below 0 for capped stats).</summary>
    public static float Capped(StatType stat, float value) =>
        Cap(stat) is { } cap ? System.Math.Clamp(value, 0f, cap) : value;

    /// <summary>A note for tooltips and cards, e.g. "Dodge caps at 60%", or null when uncapped.</summary>
    public static string CapNote(StatType stat) =>
        Cap(stat) is { } cap ? $"{Label(stat)} caps at {cap:0.#}%" : null;

    public static bool IsPercent(StatType stat) =>
        stat is StatType.Damage or StatType.AttackSpeed or StatType.MoveSpeed or StatType.CritChance
            or StatType.LifeSteal or StatType.MinionDamage or StatType.CooldownReduction or StatType.Dodge;

    /// <summary>Formats a bonus like "+15% Damage" or "-3 Max HP".</summary>
    public static string Format(StatType stat, float value)
    {
        string sign = value >= 0 ? "+" : "";
        string unit = IsPercent(stat) ? "%" : "";
        string per = stat == StatType.HpRegen ? $" per {RegenPeriod:0}s" : "";
        return $"{sign}{value:0.#}{unit} {Label(stat)}{per}";
    }

    /// <summary>A run total for the stats panel: a capped stat past its cap shows the cap, e.g. "+60% Dodge (capped)".</summary>
    public static string FormatTotal(StatType stat, float value) =>
        Cap(stat) is { } cap && value > cap ? $"{Format(stat, cap)} (capped)" : Format(stat, value);
}
