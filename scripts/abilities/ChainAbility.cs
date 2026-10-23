using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Instant bolt that strikes an enemy, then jumps to the nearest enemy not yet hit, losing
/// some damage each jump (Chain Lightning). Hits land instantly, so nothing gets reserved.
/// </summary>
[GlobalClass]
public partial class ChainAbility : AbilityData
{
    [ExportGroup("Chain")]
    /// <summary>Extra enemies after the first.</summary>
    [Export] public int Jumps { get; set; } = 4;
    [Export] public float JumpRange { get; set; } = 180f;

    /// <summary>Damage multiplier applied per jump, e.g. 0.8 = each jump hits 20% softer.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float JumpFalloff { get; set; } = 0.8f;

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        // Manual casts strike the enemy nearest the aim point, as long as it's in range.
        var first = targetEnemy;
        if (first == null)
        {
            var nearAim = Combat.NearestEnemy(ctx.Tree, ClampToRange(ctx, target), JumpRange);
            if (nearAim != null && ctx.Origin.DistanceTo(nearAim.GlobalPosition) <= EffectiveRange(ctx) + JumpRange)
                first = nearAim;
        }

        if (first == null)
        {
            // Nothing there: a fizzle toward the aim point so the cast still reads.
            LineFlash.Spawn(ctx.World, [ctx.Origin, ClampToRange(ctx, target)], IconColor, 3f, jitter: 10f);
            return;
        }

        var hit = new HashSet<Enemy>();
        var points = new List<Vector2> { ctx.Origin };
        float damage = Damage * ctx.DamageMultiplier;
        var effects = Effects(ctx);

        for (var enemy = first; enemy != null && hit.Count <= Jumps; enemy = NextTarget(ctx, enemy, hit))
        {
            hit.Add(enemy);
            points.Add(enemy.GlobalPosition);
            Combat.Hit(enemy, damage, effects, points[^2]);
            damage *= JumpFalloff;
        }

        LineFlash.Spawn(ctx.World, points.ToArray(), IconColor, 3f, jitter: 10f);
    }

    private Enemy NextTarget(AbilityContext ctx, Enemy from, HashSet<Enemy> hit)
    {
        Enemy nearest = null;
        float nearestDistSq = JumpRange * JumpRange;

        foreach (var enemy in Combat.EnemiesInRadius(ctx.Tree, from.GlobalPosition, JumpRange))
        {
            if (hit.Contains(enemy))
                continue;

            float distSq = from.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (distSq <= nearestDistSq)
            {
                nearest = enemy;
                nearestDistSq = distSq;
            }
        }

        return nearest;
    }
}
