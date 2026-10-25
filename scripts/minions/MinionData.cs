using Godot;

namespace ArenaShooter;

/// <summary>An allied unit type (Necromancer): stats and look. One .tres per type in data/minions/.</summary>
[GlobalClass]
public partial class MinionData : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Color Color { get; set; } = new("ebe5cf");

    /// <summary>Outline, plus the ring on enemies marked to rise as this minion.</summary>
    [Export] public Color GlowColor { get; set; } = new("6fe0c0");
    [Export] public float Size { get; set; } = 18f;

    [ExportGroup("Stats")]
    [Export] public float MaxHealth { get; set; } = 14f;
    [Export] public float Armor { get; set; }
    [Export] public float Speed { get; set; } = 230f;

    /// <summary>
    /// On: enemies may chase it instead of the player, their contact hurts it and enemy shots hit it
    /// (a tank). Off: enemies ignore it, it takes no damage and only its lifetime ends it.
    /// </summary>
    [Export] public bool Targetable { get; set; } = true;

    /// <summary>Seconds before it crumbles on its own.</summary>
    [Export] public float Lifetime { get; set; } = 18f;

    /// <summary>Most of this type alive at once. Summoning past it crumbles the oldest.</summary>
    [Export] public int MaxAlive { get; set; } = 6;

    [ExportGroup("Attack")]
    /// <summary>Base damage per attack; scaled by the player's Damage% + Minion Damage%.</summary>
    [Export] public float Damage { get; set; } = 4f;
    [Export] public float AttackCooldown { get; set; } = 0.7f;

    /// <summary>Gap between the two bodies' edges at which it can strike.</summary>
    [Export] public float AttackReach { get; set; } = 6f;

    /// <summary>0 = hits one enemy; otherwise every enemy this close to the target.</summary>
    [Export] public float SplashRadius { get; set; }
    [Export] public float Knockback { get; set; } = 30f;
}
