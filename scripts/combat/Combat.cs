using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Everything a hit does beyond its base damage: crit odds and secondary effects.
/// <see cref="CritChance"/> is 0–1; a crit multiplies the damage by <see cref="CritMultiplier"/>.
/// </summary>
public readonly record struct HitEffects(
    float SlowMultiplier = 1f, float SlowDuration = 0f, float Knockback = 0f,
    float CritChance = 0f, float CritMultiplier = 1f);

/// <summary>
/// Shared targeting and damage helpers. Uses simple distance checks against the enemies
/// group rather than physics queries, which is plenty for prototype enemy counts.
/// </summary>
public static class Combat
{
    /// <summary>Nearest enemy worth targeting: skips ones that damage already in flight will kill.</summary>
    public static Enemy NearestEnemy(SceneTree tree, Vector2 from, float range)
    {
        Enemy nearest = null;
        float nearestDistSq = range * range;

        foreach (var node in tree.GetNodesInGroup(Groups.Enemies))
        {
            if (node is not Enemy enemy || enemy.IsDoomed)
                continue;

            float distSq = from.DistanceSquaredTo(enemy.GlobalPosition);
            if (distSq <= nearestDistSq)
            {
                nearest = enemy;
                nearestDistSq = distSq;
            }
        }

        return nearest;
    }

    public static List<Enemy> EnemiesInRadius(SceneTree tree, Vector2 center, float radius)
    {
        var result = new List<Enemy>();
        float radiusSq = radius * radius;

        foreach (var node in tree.GetNodesInGroup(Groups.Enemies))
        {
            if (node is Enemy enemy && !enemy.Health.IsDead
                && center.DistanceSquaredTo(enemy.GlobalPosition) <= radiusSq)
            {
                result.Add(enemy);
            }
        }

        return result;
    }

    /// <summary>Enemies within <paramref name="radius"/> of the line from <paramref name="a"/> to <paramref name="b"/>.</summary>
    public static List<Enemy> EnemiesNearSegment(SceneTree tree, Vector2 a, Vector2 b, float radius)
    {
        var result = new List<Enemy>();
        float radiusSq = radius * radius;

        foreach (var node in tree.GetNodesInGroup(Groups.Enemies))
        {
            if (node is Enemy enemy && !enemy.Health.IsDead
                && Geometry2D.GetClosestPointToSegment(enemy.GlobalPosition, a, b).DistanceSquaredTo(enemy.GlobalPosition) <= radiusSq)
            {
                result.Add(enemy);
            }
        }

        return result;
    }

    /// <summary>
    /// The enemy within <paramref name="range"/> with the most other enemies within
    /// <paramref name="radius"/> of it: the best spot for an area attack. Ties go to the nearer one.
    /// </summary>
    public static Enemy DensestEnemy(SceneTree tree, Vector2 from, float range, float radius, out int count)
    {
        var candidates = EnemiesInRadius(tree, from, range);
        Enemy best = null;
        count = 0;
        float bestDistSq = float.MaxValue;
        float radiusSq = radius * radius;

        foreach (var enemy in candidates)
        {
            if (enemy.IsDoomed)
                continue;

            int n = 0;
            foreach (var other in candidates)
            {
                if (enemy.GlobalPosition.DistanceSquaredTo(other.GlobalPosition) <= radiusSq)
                    n++;
            }

            float distSq = from.DistanceSquaredTo(enemy.GlobalPosition);
            if (n > count || (n == count && distSq < bestDistSq))
            {
                best = enemy;
                count = n;
                bestDistSq = distSq;
            }
        }

        return best;
    }

    /// <summary>
    /// Applies one hit. Crit is rolled here, per enemy, so an explosion can crit some targets
    /// and not others. Damage reservations use the non-crit damage, so a crit can only make
    /// an enemy die sooner than expected, never later.
    /// </summary>
    public static void Hit(Enemy enemy, float damage, HitEffects effects, Vector2 source)
    {
        if (effects.Knockback > 0f)
            enemy.ApplyKnockback(source.DirectionTo(enemy.GlobalPosition) * effects.Knockback);
        if (effects.SlowDuration > 0f)
            enemy.ApplySlow(effects.SlowMultiplier, effects.SlowDuration);

        bool crit = GD.Randf() < effects.CritChance;
        enemy.TakeDamage(crit ? damage * effects.CritMultiplier : damage, crit);
    }

    public static void DamageArea(SceneTree tree, Vector2 center, float radius, float damage, HitEffects effects)
    {
        foreach (var enemy in EnemiesInRadius(tree, center, radius))
            Hit(enemy, damage, effects, center);
    }
}
