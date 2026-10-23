using Godot;

namespace ArenaShooter;

/// <summary>Who is casting, plus the run-stat modifiers that apply to their abilities.</summary>
public class AbilityContext
{
    public AbilityContext(Node2D caster)
    {
        Caster = caster;
    }

    public Node2D Caster { get; }
    public float DamageMultiplier { get; set; } = 1f;
    public float RangeBonus { get; set; }

    /// <summary>Percent points added to every ability's base crit chance.</summary>
    public float CritChanceBonus { get; set; }

    /// <summary>Percent points added to every ability's base life steal.</summary>
    public float LifeStealBonus { get; set; }

    /// <summary>Damage multiplier for minions: Damage% and Minion Damage% added together.</summary>
    public float MinionDamageMultiplier { get; set; } = 1f;

    public SceneTree Tree => Caster.GetTree();
    public Node World => Caster.GetTree().CurrentScene;
    public Vector2 Origin => Caster.GlobalPosition;
}
