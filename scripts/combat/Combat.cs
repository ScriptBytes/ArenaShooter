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
