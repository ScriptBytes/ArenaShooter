using Godot;

namespace ArenaShooter;

/// <summary>
/// Circular blast, either around the caster (Whirlwind, Frost Nova) or at a target point
/// within range (Ground Slam, Meteor). With a <see cref="Delay"/> the spot is telegraphed
/// first and the blast lands later, so enemies can walk out of it.
/// </summary>
[GlobalClass]
public partial class AreaAbility : AbilityData
{
    [ExportGroup("Area")]
    [Export] public float Radius { get; set; } = 100f;
    [Export] public bool CenteredOnCaster { get; set; } = true;

    /// <summary>Auto-cast waits until at least this many enemies would be hit.</summary>
    [Export] public int MinTargets { get; set; } = 1;

    /// <summary>Seconds between casting and the blast landing. 0 = instant.</summary>
    [Export] public float Delay { get; set; }

    // When auto-cast fires, for spells centered on the caster (buffs, novas) and Blink.
    [ExportGroup("Auto-cast")]
    /// <summary>
    /// Pixels. Above 0, auto-cast counts <see cref="MinTargets"/> within this distance instead of
    /// <see cref="Radius"/>: a buff whose blast is small but whose payoff reaches further (Blood Pact
    /// boosts a ranged weapon, so it should fire once enemies are in weapon range).
    /// </summary>
    [Export] public float CastWhenEnemiesWithin { get; set; }

    /// <summary>
    /// Seconds. Above 0, auto-cast also fires (with fewer than <see cref="MinTargets"/> nearby) when an
    /// enemy hit, or tried to hit, the caster this recently and at least one is still within range:
    /// defensive spells react to danger rather than waiting for a crowd.
    /// </summary>
    [Export] public float CastWhenHitWithin { get; set; }

    /// <summary>The distance auto-cast counts enemies within.</summary>
    protected float AutoCastRadius => CastWhenEnemiesWithin > 0f ? CastWhenEnemiesWithin : Radius;

    /// <summary>True when the caster was hit within <see cref="CastWhenHitWithin"/> seconds.</summary>
    protected bool RecentlyHit(AbilityContext ctx) =>
        CastWhenHitWithin > 0f && ctx.Caster.GetParent() is Player p && p.SecondsSinceHit <= CastWhenHitWithin;

    public override bool NeedsAim => !CenteredOnCaster;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        if (!CenteredOnCaster)
        {
            // Aim at the thickest part of the crowd, not just the nearest enemy.
            targetEnemy = Combat.DensestEnemy(ctx.Tree, ctx.Origin, EffectiveRange(ctx), Radius, out int count);
            target = targetEnemy?.GlobalPosition ?? ctx.Origin;
            return targetEnemy != null && count >= MinTargets;
        }

        target = ctx.Origin;
        targetEnemy = null;
        int near = Combat.EnemiesInRadius(ctx.Tree, ctx.Origin, AutoCastRadius).Count;
        return near >= MinTargets || (near > 0 && RecentlyHit(ctx));
    }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        var center = CenteredOnCaster ? ctx.Origin : ClampToRange(ctx, target);
        if (Delay > 0f)
            DelayedBlast.Spawn(ctx.World, center, Radius, IconColor, Delay, () => Blast(ctx, center));
        else
            Blast(ctx, center);
    }

    protected void Blast(AbilityContext ctx, Vector2 center)
    {
        Combat.DamageArea(ctx.Tree, center, Radius, Damage * ctx.DamageMultiplier, Effects(ctx));
        AreaFlash.Spawn(ctx.World, center, Radius, IconColor);
        if (ScreenShake > 0f)
            ShakeCamera.Shake(ctx.Tree, ScreenShake);
    }
}
