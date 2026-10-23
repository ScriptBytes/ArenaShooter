using Godot;

namespace ArenaShooter;

/// <summary>
/// Dash along the ground toward the target, hitting every enemy in the path and shoving it
/// aside (Charge). Untouchable while dashing; walls cut it short. Moves the player, so it
/// should default to manual cast.
/// </summary>
[GlobalClass]
public partial class ChargeAbility : AbilityData
{
    [ExportGroup("Charge")]
    [Export] public float DashDuration { get; set; } = 0.2f;

    /// <summary>Half-width of the path: enemies this close to the line get hit.</summary>
    [Export] public float HitRadius { get; set; } = 40f;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        // Charge through the nearest enemy and out the other side, not just up to it.
        bool found = base.TryAutoTarget(ctx, out target, out targetEnemy);
        if (found)
            target = ctx.Origin + ctx.Origin.DirectionTo(target) * EffectiveRange(ctx);
        return found;
    }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        var start = ctx.Origin;
        var end = ClampToRange(ctx, target);
        if (end == start)
            end = start + Vector2.Right * EffectiveRange(ctx);

        if (ctx.Caster.GetParent() is not Player player)
        {
            Strike(ctx, start, end);
            return;
        }

        player.Leap(end, DashDuration, () => Strike(ctx, start, player.GlobalPosition), airborne: false);
    }

    private void Strike(AbilityContext ctx, Vector2 start, Vector2 end)
    {
        var effects = Effects(ctx);
        foreach (var enemy in Combat.EnemiesNearSegment(ctx.Tree, start, end, HitRadius))
        {
            // Shove away from the path, so the charge carves a lane through the crowd.
            var source = Geometry2D.GetClosestPointToSegment(enemy.GlobalPosition, start, end);
            Combat.Hit(enemy, Damage * ctx.DamageMultiplier, effects, source);
        }

        LineFlash.Spawn(ctx.World, [start, end], IconColor, HitRadius);
        if (ScreenShake > 0f)
            ShakeCamera.Shake(ctx.Tree, ScreenShake);
    }
}
