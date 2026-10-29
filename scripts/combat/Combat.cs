using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Everything a hit does beyond its base damage: crit odds and secondary effects.
/// <see cref="CritChance"/> is 0–1; a crit multiplies the damage by <see cref="CritMultiplier"/>.
/// <see cref="LifeSteal"/> (0–1) heals the player for that share of the damage that landed.
/// <see cref="DotDamage"/> is per second. <see cref="RaiseChance"/> (0–1) is the odds a marked
/// enemy rises as <see cref="RaiseMinion"/> if it dies within <see cref="RaiseWindow"/> seconds.
/// <see cref="StunDuration"/> stops the enemy moving and shooting. <see cref="DotSpreads"/> makes the
/// DoT jump to that many nearby enemies when its host dies. <see cref="Source"/> names what dealt the
/// hit (an ability or minion), for <see cref="Combat.DamageBySource"/>.
/// </summary>
public readonly record struct HitEffects(
    float SlowMultiplier = 1f, float SlowDuration = 0f, float Knockback = 0f,
    float CritChance = 0f, float CritMultiplier = 1f, float LifeSteal = 0f,
    float DotDamage = 0f, float DotDuration = 0f, Color DotColor = default,
    MinionData RaiseMinion = null, float RaiseChance = 0f, float RaiseWindow = 0f,
    float StunDuration = 0f, int DotSpreads = 0, string Source = null);

/// <summary>
/// Shared targeting and damage helpers. Uses simple distance checks against the enemies
/// group rather than physics queries, which is plenty for prototype enemy counts.
/// </summary>
public static class Combat
{
    /// <summary>
    /// Damage that landed (no overkill) per source name since the last <see cref="ResetDamageLog"/>.
    /// Autoplay logs it at run end; it's how spells are compared when tuning.
    /// </summary>
    public static Dictionary<string, float> DamageBySource { get; } = new();

    /// <summary><see cref="DamageBySource"/> as it stood when the current wave started, for "this wave" totals.</summary>
    public static Dictionary<string, float> DamageAtWaveStart { get; } = new();

    /// <summary>Bar color per source name in the damage meter (weapons, spells, minion types).</summary>
    public static Dictionary<string, Color> SourceColors { get; } = new();

    public static void ResetDamageLog()
    {
        DamageBySource.Clear();
        DamageAtWaveStart.Clear();
        SourceColors.Clear();
    }

    public static void MarkWaveStart()
    {
        DamageAtWaveStart.Clear();
        foreach (var (source, damage) in DamageBySource)
            DamageAtWaveStart[source] = damage;
    }

    /// <summary>Gives <paramref name="ability"/>'s damage, and that of the minions it summons or raises, a meter color.</summary>
    public static void RegisterSource(AbilityData ability)
    {
        if (ability == null)
            return;
        SourceColors[ability.DisplayName] = ability.IconColor;
        if (ability is SummonAbility { Minion: { } summoned })
            SourceColors[summoned.DisplayName] = summoned.GlowColor;
        if (ability.RaiseMinion is { } raised)
            SourceColors[raised.DisplayName] = raised.GlowColor;
    }
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
        if (effects.StunDuration > 0f)
            enemy.ApplyStun(effects.StunDuration);
        if (effects.DotDamage > 0f && effects.DotDuration > 0f)
            enemy.ApplyDot(effects.DotDamage, effects.DotDuration, effects.LifeSteal, effects.DotColor,
                effects.DotSpreads, effects.Source);
        // Marked before the damage lands, so the killing blow itself can raise the enemy.
        if (effects.RaiseMinion != null && effects.RaiseChance > 0f)
            enemy.MarkForRaising(effects.RaiseMinion, effects.RaiseChance, effects.RaiseWindow);

        bool crit = GD.Randf() < effects.CritChance;
        Deal(enemy, crit ? damage * effects.CritMultiplier : damage, effects.LifeSteal, crit, source: effects.Source);
    }

    /// <summary>
    /// Damages an enemy with no side effects, and heals the player for <paramref name="lifeSteal"/>
    /// (0–1) of the damage that landed (overkill doesn't count). The life steal hook: every
    /// player hit, DoT tick included, ends up here.
    /// </summary>
    public static void Deal(Enemy enemy, float damage, float lifeSteal, bool crit = false, Color? color = null,
        string source = null)
    {
        float landed = Mathf.Min(damage, enemy.Health.Current);
        if (landed > 0f && !enemy.Health.IsDead)
        {
            string key = source ?? "other";
            DamageBySource[key] = DamageBySource.GetValueOrDefault(key) + landed;
        }
        var player = lifeSteal > 0f ? enemy.GetTree().GetFirstNodeInGroup(Groups.Player) as Player : null;

        enemy.TakeDamage(damage, crit, color);
        if (player != null && landed > 0f)
            player.StealLife(landed * lifeSteal);
    }

    public static void DamageArea(SceneTree tree, Vector2 center, float radius, float damage, HitEffects effects)
    {
        foreach (var enemy in EnemiesInRadius(tree, center, radius))
            Hit(enemy, damage, effects, center);
    }
}
