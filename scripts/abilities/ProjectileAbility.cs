using Godot;

namespace ArenaShooter;

/// <summary>
/// Fires a projectile toward the target. With an explosion radius it detonates on the
/// first hit or when it reaches the target point (so manual casts land where you aim).
/// Options: a volley of <see cref="Count"/> shots fanned over <see cref="SpreadDegrees"/>,
/// <see cref="Pierce"/> (hits everything on its path once), <see cref="Returns"/> (flies back to
/// the caster, hitting again: Throwing Axe), and <see cref="Homing"/> (each shot seeks its own
/// enemy: Arcane Missiles, Vampire Bats).
/// </summary>
[GlobalClass]
public partial class ProjectileAbility : AbilityData
{
    // Manual homing casts look for enemies this close to the aim point.
    private const float AimSearchRadius = 220f;

    [ExportGroup("Projectile")]
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public float ProjectileSpeed { get; set; } = 650f;
    [Export] public float ProjectileRadius { get; set; } = 5f;
    [Export] public float ExplosionRadius { get; set; }
    [Export] public ProjectileShape Shape { get; set; } = ProjectileShape.Orb;

    /// <summary>Shots per cast. Each homing shot picks its own enemy, so a volley spreads over a crowd.</summary>
    [Export] public int Count { get; set; } = 1;

    /// <summary>The fan the volley leaves in, in degrees (centered on the aim).</summary>
    [Export] public float SpreadDegrees { get; set; }

    [Export] public bool Pierce { get; set; }

    /// <summary>Flies the full range, then back to the caster. Implies piercing.</summary>
    [Export] public bool Returns { get; set; }

    /// <summary>Turn rate (degrees per second) toward the shot's enemy. 0 = straight.</summary>
    [Export] public float Homing { get; set; }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        float range = EffectiveRange(ctx);
        var aim = ctx.Origin.DirectionTo(target);
        if (aim == Vector2.Zero)
            aim = Vector2.Right;

        for (int i = 0; i < Count; i++)
        {
            float t = Count == 1 ? 0f : (float)i / (Count - 1) - 0.5f;
            var direction = aim.Rotated(Mathf.DegToRad(SpreadDegrees) * t);

            // Homing: the first shot takes the auto-cast target; the rest take the nearest enemy not
            // already doomed by the shots before them. Manual casts search around the aim point.
            Enemy seek = null;
            if (Homing > 0f)
            {
                seek = i == 0 && targetEnemy != null ? targetEnemy
                    : Combat.NearestEnemy(ctx.Tree, targetEnemy != null ? ctx.Origin : ClampToRange(ctx, target),
                        targetEnemy != null ? range : AimSearchRadius);
            }
            else if (i == 0)
            {
                seek = targetEnemy;
            }

            Fire(ctx, direction, target, range, seek);
        }
    }

    private void Fire(AbilityContext ctx, Vector2 direction, Vector2 target, float range, Enemy seek)
    {
        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.Direction = direction;
        projectile.Rotation = direction.Angle();
        projectile.Speed = ProjectileSpeed;
        projectile.Damage = Damage * ctx.DamageMultiplier;
        projectile.Radius = ProjectileRadius;
        projectile.Color = IconColor;
        projectile.Shape = Shape;
        projectile.ExplosionRadius = ExplosionRadius;
        projectile.Effects = Effects(ctx);
        projectile.ScreenShake = ScreenShake;
        projectile.Pierce = Pierce || Returns;
        projectile.ReturnTo = Returns ? ctx.Caster : null;
        projectile.Homing = Mathf.DegToRad(Homing);
        projectile.HomingTarget = Homing > 0f ? seek : null;
        projectile.MaxDistance = ExplosionRadius > 0f ? Mathf.Min(ctx.Origin.DistanceTo(target), range)
            : Returns ? range
            : Homing > 0f ? range * 2f
            : range * 1.25f;

        // Projectiles live in the world, not under the caster, so they don't move with it.
        ctx.World.AddChild(projectile);
        projectile.GlobalPosition = ctx.Origin;
        // A piercing shot doesn't stop at its target, so it doesn't reserve the kill.
        if (seek != null && !projectile.Pierce)
            projectile.ReserveOn(seek);
    }
}
