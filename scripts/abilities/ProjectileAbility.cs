using Godot;

namespace ArenaShooter;

/// <summary>
/// Fires a projectile toward the target. With an explosion radius it detonates on the
/// first hit or when it reaches the target point (so manual casts land where you aim).
/// </summary>
[GlobalClass]
public partial class ProjectileAbility : AbilityData
{
    [ExportGroup("Projectile")]
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public float ProjectileSpeed { get; set; } = 650f;
    [Export] public float ProjectileRadius { get; set; } = 5f;
    [Export] public float ExplosionRadius { get; set; }

    public override void Cast(AbilityContext ctx, Vector2 target)
    {
        float range = EffectiveRange(ctx);
        var direction = ctx.Origin.DirectionTo(target);
        if (direction == Vector2.Zero)
            direction = Vector2.Right;

        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.Direction = direction;
        projectile.Speed = ProjectileSpeed;
        projectile.Damage = Damage * ctx.DamageMultiplier;
        projectile.Radius = ProjectileRadius;
        projectile.Color = IconColor;
        projectile.ExplosionRadius = ExplosionRadius;
        projectile.Effects = Effects;
        projectile.ScreenShake = ScreenShake;
        projectile.MaxDistance = ExplosionRadius > 0f
            ? Mathf.Min(ctx.Origin.DistanceTo(target), range)
            : range * 1.25f;

        // Projectiles live in the world, not under the caster, so they don't move with it.
        ctx.World.AddChild(projectile);
        projectile.GlobalPosition = ctx.Origin;
    }
}
