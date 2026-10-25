using Godot;

namespace ArenaShooter;

/// <summary>
/// A lasting damage zone (<see cref="AuraField"/>) that ticks every <see cref="TickInterval"/> for
/// <see cref="Duration"/> seconds. <see cref="AbilityData.Damage"/> is per second. Centered on the
/// caster it follows them around (Siphoning Aura); otherwise it stays where it was cast, on the
/// thickest crowd (Blizzard). On-hit effects (slow, life steal, DoT) apply on every tick.
/// </summary>
[GlobalClass]
public partial class AuraAbility : AreaAbility
{
    [ExportGroup("Aura")]
    [Export] public float Duration { get; set; } = 4f;
    [Export] public float TickInterval { get; set; } = 0.5f;

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        var center = CenteredOnCaster ? ctx.Origin : ClampToRange(ctx, target);
        AuraField.Spawn(ctx.World, ctx, this, center, Effects(ctx));
    }
}
