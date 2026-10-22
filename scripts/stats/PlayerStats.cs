using System.Collections.Generic;

namespace ArenaShooter;

/// <summary>
/// Accumulated stat bonuses for the current run. Entities combine these with their own
/// base values (see Player.ApplyStats / Weapon.ApplyStats).
/// </summary>
public class PlayerStats
{
    private readonly Dictionary<StatType, float> _bonuses = new();

    public float Get(StatType stat) => _bonuses.GetValueOrDefault(stat);

    public void Add(StatType stat, float value) => _bonuses[stat] = Get(stat) + value;

    /// <summary>Multiplier for a percent stat, e.g. +15 → 1.15. Floored so stats can't go negative.</summary>
    public float Multiplier(StatType stat, float floor = 0.1f) =>
        System.MathF.Max(1f + Get(stat) / 100f, floor);
}
