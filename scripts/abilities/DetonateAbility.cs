using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Blows up up to <see cref="MaxMinions"/> of the caster's minions (Corpse Explosion): each bursts
/// for <see cref="AbilityData.Damage"/> in <see cref="Radius"/> and is gone. Only types with
/// <see cref="MinionData.Detonatable"/> count. Damage scales like minions do (Damage% + Minion Damage%).
/// Manual casts pick the minions nearest the aim point; auto-cast waits until one has at least
/// <see cref="MinTargets"/> enemies around it and starts from that one.
/// </summary>
[GlobalClass]
public partial class DetonateAbility : AbilityData
{
    [ExportGroup("Detonate")]
    [Export] public float Radius { get; set; } = 90f;
    [Export] public int MaxMinions { get; set; } = 3;
    [Export] public int MinTargets { get; set; } = 2;

    private static IEnumerable<Minion> Detonatable(SceneTree tree) =>
        tree.GetNodesInGroup(Groups.Minions).OfType<Minion>()
            .Where(m => m.Data.Detonatable && !m.IsQueuedForDeletion());

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        targetEnemy = null;
        target = ctx.Origin;
        int best = 0;
        foreach (var minion in Detonatable(ctx.Tree))
        {
            int count = Combat.EnemiesInRadius(ctx.Tree, minion.GlobalPosition, Radius).Count;
            if (count > best)
            {
                best = count;
                target = minion.GlobalPosition;
            }
        }
        return best >= MinTargets;
    }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        var chosen = Detonatable(ctx.Tree)
            .OrderBy(m => m.GlobalPosition.DistanceSquaredTo(target))
            .Take(MaxMinions)
            .ToList();

        if (chosen.Count == 0)
        {
            // No minions: a puff at the aim point so the key press still reads.
            AreaFlash.Spawn(ctx.World, ClampToRange(ctx, target), 20f, IconColor);
            return;
        }

        float damage = Damage * ctx.MinionDamageMultiplier;
        var effects = Effects(ctx);
        foreach (var minion in chosen)
            minion.Detonate(damage, Radius, effects);
        if (ScreenShake > 0f)
            ShakeCamera.Shake(ctx.Tree, ScreenShake);
    }
}
