using Godot;

namespace ArenaShooter;

/// <summary>
/// Tethers the caster to an enemy with a beam that deals <see cref="AbilityData.Damage"/> per
/// second for <see cref="Duration"/> seconds (Drain Life). Pair it with LifeSteal to heal.
/// If the target dies or gets out of reach, the beam jumps to the next nearest enemy.
/// </summary>
[GlobalClass]
public partial class DrainAbility : AbilityData
{
    [ExportGroup("Drain")]
    [Export] public float Duration { get; set; } = 3f;
    [Export] public float TickInterval { get; set; } = 0.25f;

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        // Manual casts latch onto the enemy nearest the aim point, if it's in range.
        var first = targetEnemy;
        if (first == null)
        {
            var nearAim = Combat.NearestEnemy(ctx.Tree, ClampToRange(ctx, target), 120f);
            if (nearAim != null && ctx.Origin.DistanceTo(nearAim.GlobalPosition) <= EffectiveRange(ctx))
                first = nearAim;
        }

        if (first == null)
        {
            LineFlash.Spawn(ctx.World, [ctx.Origin, ClampToRange(ctx, target)], IconColor, 3f, jitter: 6f);
            return;
        }

        DrainBeam.Spawn(ctx.World, ctx, this, first, Effects(ctx));
    }
}
