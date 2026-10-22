using Godot;

namespace ArenaShooter;

/// <summary>
/// Instant circular blast, either around the caster (Whirlwind, Frost Nova) or at a
/// target point within range (Ground Slam).
/// </summary>
[GlobalClass]
public partial class AreaAbility : AbilityData
{
    [ExportGroup("Area")]
    [Export] public float Radius { get; set; } = 100f;
    [Export] public bool CenteredOnCaster { get; set; } = true;

    /// <summary>Auto-cast waits until at least this many enemies would be hit.</summary>
    [Export] public int MinTargets { get; set; } = 1;

    public override bool NeedsAim => !CenteredOnCaster;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target)
    {
        if (!CenteredOnCaster)
            return base.TryAutoTarget(ctx, out target);

        target = ctx.Origin;
        return Combat.EnemiesInRadius(ctx.Tree, ctx.Origin, Radius).Count >= MinTargets;
    }

    public override void Cast(AbilityContext ctx, Vector2 target)
    {
        Blast(ctx, CenteredOnCaster ? ctx.Origin : ClampToRange(ctx, target));
    }

    protected void Blast(AbilityContext ctx, Vector2 center)
    {
        Combat.DamageArea(ctx.Tree, center, Radius, Damage * ctx.DamageMultiplier, Effects);
        AreaFlash.Spawn(ctx.World, center, Radius, IconColor);
        if (ScreenShake > 0f)
            ShakeCamera.Shake(ctx.Tree, ScreenShake);
    }
}
