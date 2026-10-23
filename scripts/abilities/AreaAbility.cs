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
        return Combat.EnemiesInRadius(ctx.Tree, ctx.Origin, Radius).Count >= MinTargets;
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
