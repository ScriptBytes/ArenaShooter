using Godot;

namespace ArenaShooter;

/// <summary>
/// A near-instant hop to the aim point (Blink) that leaves a blast where the caster *was*, so it's
/// an escape rather than an engage (<see cref="LeapAbility"/> blasts where it lands). Moves the
/// player: manual by default. Auto-cast (only with it switched on) fires when the caster is cornered:
/// <see cref="AreaAbility.MinTargets"/> enemies within the radius, or one after a recent hit
/// (<see cref="AreaAbility.CastWhenHitWithin"/>). It then picks the safest landing spot inside the
/// arena, and holds the cooldown when no spot in range is clearly safer than staying.
/// </summary>
[GlobalClass]
public partial class BlinkAbility : LeapAbility
{
    private const int Directions = 16;

    /// <summary>Enemies this close to a landing spot make it dangerous (closer counts more).</summary>
    private const float DangerRadius = 180f;

    /// <summary>Landing spots stay this far inside the arena edges, so a blink doesn't end in a corner.</summary>
    private const float EdgeMargin = 60f;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        targetEnemy = null;
        target = ctx.Origin;
        int near = Combat.EnemiesInRadius(ctx.Tree, ctx.Origin, AutoCastRadius).Count;
        if (near == 0 || (near < MinTargets && !RecentlyHit(ctx)))
            return false;

        var enemies = Combat.EnemiesInRadius(ctx.Tree, ctx.Origin, EffectiveRange(ctx) + DangerRadius);
        var bounds = ctx.Tree.GetFirstNodeInGroup(Groups.Arena) is Arena arena
            ? arena.Bounds.Grow(-EdgeMargin)
            : new Rect2(ctx.Origin - Vector2.One * 1e5f, Vector2.One * 2e5f);

        float range = EffectiveRange(ctx);
        float bestDanger = float.MaxValue;
        for (int i = 0; i < Directions; i++)
        {
            var dir = Vector2.Right.Rotated(Mathf.Tau * i / Directions);
            var spot = (ctx.Origin + dir * range).Clamp(bounds.Position, bounds.End);
            // A spot the walls cut short is barely an escape.
            if (spot.DistanceTo(ctx.Origin) < range * 0.5f)
                continue;

            float danger = Danger(enemies, spot);
            if (danger < bestDanger)
            {
                bestDanger = danger;
                target = spot;
            }
        }

        // Only worth the cooldown when the landing is clearly safer than where we stand.
        return bestDanger < Danger(enemies, ctx.Origin) * 0.5f;
    }

    private static float Danger(System.Collections.Generic.List<Enemy> enemies, Vector2 spot)
    {
        float danger = 0f;
        foreach (var enemy in enemies)
        {
            float closeness = 1f - spot.DistanceTo(enemy.GlobalPosition) / DangerRadius;
            if (closeness > 0f)
                danger += closeness * closeness;
        }
        return danger;
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
