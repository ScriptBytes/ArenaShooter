using Godot;

namespace ArenaShooter;

/// <summary>
/// Raises <see cref="Count"/> minions around the caster (Raise Skeletons, Bone Golem).
/// Auto-casts once an enemy is within range, so summons aren't wasted on an empty arena.
/// The cap lives on the minion type (<see cref="MinionData.MaxAlive"/>), shared with raises from kills.
/// </summary>
[GlobalClass]
public partial class SummonAbility : AbilityData
{
    [ExportGroup("Summon")]
    [Export] public MinionData Minion { get; set; }
    [Export] public int Count { get; set; } = 1;

    public override bool NeedsAim => false;

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        for (int i = 0; i < Count; i++)
        {
            var offset = Vector2.FromAngle(Mathf.Tau * i / Count + (float)GD.RandRange(0.0, Mathf.Tau)) * 40f;
            ArenaShooter.Minion.Spawn(ctx.World, Minion, ctx.Origin + offset);
        }
    }
}
