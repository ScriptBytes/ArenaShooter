using Godot;

namespace ArenaShooter;

/// <summary>
/// Leap to a point within range, then blast the landing area. The caster is
/// untouchable mid-air. Falls back to a plain blast if the caster can't leap.
/// </summary>
[GlobalClass]
public partial class LeapAbility : AreaAbility
{
    [ExportGroup("Leap")]
    [Export] public float LeapDuration { get; set; } = 0.35f;

    public override bool NeedsAim => true;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target)
    {
        var enemy = Combat.NearestEnemy(ctx.Tree, ctx.Origin, EffectiveRange(ctx));
        target = enemy?.GlobalPosition ?? ctx.Origin;
        return enemy != null;
    }

    public override void Cast(AbilityContext ctx, Vector2 target)
    {
        var landing = ClampToRange(ctx, target);

        if (ctx.Caster.GetParent() is not Player player)
        {
            Blast(ctx, landing);
            return;
        }

        player.Leap(landing, LeapDuration, () => Blast(ctx, player.GlobalPosition));
    }
}
