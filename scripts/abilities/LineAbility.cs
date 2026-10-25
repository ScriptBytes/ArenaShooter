using Godot;

namespace ArenaShooter;

/// <summary>
/// Instant strike along a straight line from the caster, the full range toward the target,
/// hitting every enemy within <see cref="HalfWidth"/> of it (Shockwave). Unlike
/// <see cref="ChargeAbility"/> the caster stays put. Auto-cast aims through the thickest crowd
/// in range, and waits for <see cref="MinTargets"/> enemies on the line.
/// </summary>
[GlobalClass]
public partial class LineAbility : AbilityData
{
    [ExportGroup("Line")]
    [Export] public float HalfWidth { get; set; } = 35f;
    [Export] public int MinTargets { get; set; } = 1;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        float range = EffectiveRange(ctx);
        targetEnemy = Combat.DensestEnemy(ctx.Tree, ctx.Origin, range, HalfWidth * 2f, out _);
        target = targetEnemy?.GlobalPosition ?? ctx.Origin;
        if (targetEnemy == null)
            return false;

        var end = ctx.Origin + ctx.Origin.DirectionTo(target) * range;
        return Combat.EnemiesNearSegment(ctx.Tree, ctx.Origin, end, HalfWidth).Count >= MinTargets;
    }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        var direction = ctx.Origin.DirectionTo(target);
        if (direction == Vector2.Zero)
            direction = Vector2.Right;
        var start = ctx.Origin;
        var end = start + direction * EffectiveRange(ctx);

        var effects = Effects(ctx);
        foreach (var enemy in Combat.EnemiesNearSegment(ctx.Tree, start, end, HalfWidth))
            Combat.Hit(enemy, Damage * ctx.DamageMultiplier, effects, start);

        // A wide faint band for the hit area, and a jagged crack down the middle.
        LineFlash.Spawn(ctx.World, [start, end], IconColor, HalfWidth * 2f - 6f, opacity: 0.4f, duration: 0.35f);
        LineFlash.Spawn(ctx.World, [start, end], IconColor, 4f, jitter: 12f, duration: 0.35f);
        if (ScreenShake > 0f)
            ShakeCamera.Shake(ctx.Tree, ScreenShake);
    }
}
