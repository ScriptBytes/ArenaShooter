using Godot;

namespace ArenaShooter;

/// <summary>
/// A near-instant hop to the aim point (Blink) that leaves a blast where the caster *was*, so it's
/// an escape rather than an engage (<see cref="LeapAbility"/> blasts where it lands). Moves the
/// player: manual by default. Auto-cast (only with it switched on) jumps away from the crowd once
/// <see cref="AreaAbility.MinTargets"/> enemies are within the radius.
/// </summary>
[GlobalClass]
public partial class BlinkAbility : LeapAbility
{
    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        targetEnemy = null;
        target = ctx.Origin;
        var near = Combat.EnemiesInRadius(ctx.Tree, ctx.Origin, Radius);
        if (near.Count < MinTargets)
            return false;

        // Away from the crowd's average position.
        var away = Vector2.Zero;
        foreach (var enemy in near)
            away += ctx.Origin - enemy.GlobalPosition;
        if (away == Vector2.Zero)
            away = Vector2.Right;
        target = ctx.Origin + away.Normalized() * EffectiveRange(ctx);
        return true;
    }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        var start = ctx.Origin;
        var landing = ClampToRange(ctx, target);
        Blast(ctx, start);

        if (ctx.Caster.GetParent() is not Player player)
            return;

        player.Leap(landing, LeapDuration, () =>
            AreaFlash.Spawn(ctx.World, player.GlobalPosition, player.Radius * 2.5f, IconColor), airborne: false);
        LineFlash.Spawn(ctx.World, [start, landing], IconColor, 3f, opacity: 0.6f);
    }
}
