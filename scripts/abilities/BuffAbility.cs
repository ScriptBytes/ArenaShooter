using Godot;

namespace ArenaShooter;

/// <summary>
/// Gives the caster timed stat bonuses (War Cry, Shield Wall), plus an optional blast around
/// them: set Damage to 0 for a pure shove. Auto-casts once <see cref="AreaAbility.MinTargets"/>
/// enemies are within the radius (or <see cref="AreaAbility.CastWhenEnemiesWithin"/>), or right after
/// a hit with <see cref="AreaAbility.CastWhenHitWithin"/>, so the buff isn't wasted on an empty arena.
/// A <see cref="HealthCost"/> makes it a sacrifice (Blood Pact): auto-cast then also waits
/// until paying it would still leave the caster above half health.
/// <see cref="Shield"/> grants a damage-absorbing barrier for the duration (Bone Armor), and
/// <see cref="CastBelowHealth"/> makes auto-cast wait until the caster is hurt (Second Wind).
/// <see cref="ShieldPerMaxHealth"/> and <see cref="HealPerMaxHealth"/> scale with max HP, so a
/// defensive spell stays relevant as enemy damage grows with the waves.
/// </summary>
[GlobalClass]
public partial class BuffAbility : AreaAbility
{
    [ExportGroup("Buff")]
    [Export] public float Duration { get; set; } = 5f;
    [Export] public Godot.Collections.Array<StatModifier> Modifiers { get; set; } = new();

    /// <summary>HP paid to cast. Can't kill: the caster keeps at least 1 HP.</summary>
    [Export] public float HealthCost { get; set; }

    /// <summary>HP of damage a barrier absorbs (after armor) until it breaks or the duration ends.</summary>
    [Export] public float Shield { get; set; }

    /// <summary>0–1. Adds this fraction of the caster's max HP to <see cref="Shield"/>, so the barrier keeps up with late waves.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float ShieldPerMaxHealth { get; set; }

    /// <summary>0–1. Heals this fraction of the caster's max HP at once when cast (Second Wind).</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float HealPerMaxHealth { get; set; }

    /// <summary>0–1. Above 0, auto-cast only fires once the caster's HP is below this fraction.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float CastBelowHealth { get; set; }

    public override bool NeedsAim => false;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        if (HealthCost > 0f && ctx.Caster.GetParent() is Player p && p.Health.Current - HealthCost < p.Health.Max * 0.5f)
        {
            target = ctx.Origin;
            targetEnemy = null;
            return false;
        }
        if (CastBelowHealth > 0f && ctx.Caster.GetParent() is Player hurt && hurt.Health.Current >= hurt.Health.Max * CastBelowHealth)
        {
            target = ctx.Origin;
            targetEnemy = null;
            return false;
        }

        return base.TryAutoTarget(ctx, out target, out targetEnemy);
    }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        Blast(ctx, ctx.Origin);

        if (ctx.Caster.GetParent() is not Player player)
            return;

        if (HealthCost > 0f)
            player.PayHealth(HealthCost);

        if (HealPerMaxHealth > 0f)
            player.Heal(player.Health.Max * HealPerMaxHealth, DisplayName, IconColor);

        float shield = Shield + player.Health.Max * ShieldPerMaxHealth;
        if (shield > 0f)
            player.AddShield(shield, Duration, IconColor, DisplayName);
        // A pure barrier draws itself; a ring for it would outlive a broken shield.
        if (Modifiers.Count > 0)
            player.AddBuff(this, Modifiers, Duration, IconColor);
        FloatingText.Spawn(ctx.World, ctx.Origin + new Vector2(0, -player.Radius - 12f), DisplayName, IconColor, 16);
    }
}
