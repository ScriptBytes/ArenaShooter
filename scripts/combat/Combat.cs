using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>Secondary effects a hit can apply on top of damage.</summary>
public readonly record struct HitEffects(float SlowMultiplier = 1f, float SlowDuration = 0f, float Knockback = 0f);

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

    public static void Hit(Enemy enemy, float damage, HitEffects effects, Vector2 source)
    {
        if (effects.Knockback > 0f)
            enemy.ApplyKnockback(source.DirectionTo(enemy.GlobalPosition) * effects.Knockback);
        if (effects.SlowDuration > 0f)
            enemy.ApplySlow(effects.SlowMultiplier, effects.SlowDuration);

        enemy.TakeDamage(damage);
    }

    public static void DamageArea(SceneTree tree, Vector2 center, float radius, float damage, HitEffects effects)
    {
        foreach (var enemy in EnemiesInRadius(tree, center, radius))
            Hit(enemy, damage, effects, center);
    }
}
