using Godot;

namespace ArenaShooter;

/// <summary>Placeholder silhouettes so enemy types are readable at a glance. Stored by number in .tres.</summary>
public enum EnemyShape
{
    Square = 0,
    Triangle = 1,
    Diamond = 2,
    Hexagon = 3,
}

/// <summary>
/// An enemy type (one .tres per type in data/enemies/). All types share Enemy.tscn;
/// the spawner hands each new enemy its data before adding it to the tree.
/// </summary>
[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public EnemyShape Shape { get; set; }
    [Export] public Color Color { get; set; } = new("e5484d");
    [Export] public float Size { get; set; } = 24f;

    /// <summary>Draws a health bar once damaged. For tanky types, where progress isn't obvious.</summary>
    [Export] public bool ShowHealthBar { get; set; }

    [ExportGroup("Stats")]
    [Export] public float MaxHealth { get; set; } = 10f;
    [Export] public float Speed { get; set; } = 90f;
    [Export] public float ContactDamage { get; set; } = 5f;
    [Export] public int GoldValue { get; set; } = 1;
    [Export(PropertyHint.Range, "0,1,0.05")] public float KnockbackResistance { get; set; }

    /// <summary>Chance (0–1) to also drop a heart on death.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float HeartDropChance { get; set; } = 0.03f;

    [ExportGroup("Spawning")]
    [Export] public int FirstWave { get; set; } = 1;
    [Export] public float SpawnWeight { get; set; } = 10f;

    /// <summary>Ranged enemies keep their distance and shoot. ShotRange 0 = melee only.</summary>
    [ExportGroup("Ranged Attack")]
    [Export] public float PreferredDistance { get; set; }
    [Export] public float ShotRange { get; set; }
    [Export] public float ShotInterval { get; set; } = 2f;
    [Export] public float ShotDamage { get; set; } = 4f;
    [Export] public float ShotSpeed { get; set; } = 260f;

    public bool IsRanged => ShotRange > 0f;
}
