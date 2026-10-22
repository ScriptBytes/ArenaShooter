using Godot;

namespace ArenaShooter;

/// <summary>
/// Auto-aiming weapon (Brotato-style): fires at the nearest enemy in range on a cooldown.
/// Attach as a child of whatever carries it.
/// </summary>
public partial class Weapon : Node2D
{
    [Export] public PackedScene ProjectileScene { get; set; }
    [Export] public float FireInterval { get; set; } = 0.4f;
    [Export] public float Range { get; set; } = 420f;
    [Export] public float Damage { get; set; } = 5f;
    [Export] public float ProjectileSpeed { get; set; } = 650f;

    private float _cooldown;

    public override void _PhysicsProcess(double delta)
    {
        _cooldown -= (float)delta;
        if (_cooldown > 0f)
            return;

        var target = FindNearestEnemy();
        if (target == null)
            return;

        Fire(GlobalPosition.DirectionTo(target.GlobalPosition));
        _cooldown = FireInterval;
    }

    private Node2D FindNearestEnemy()
    {
        Node2D nearest = null;
        float nearestDistSq = Range * Range;

        foreach (var node in GetTree().GetNodesInGroup(Groups.Enemies))
        {
            if (node is not Node2D enemy)
                continue;

            float distSq = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (distSq <= nearestDistSq)
            {
                nearest = enemy;
                nearestDistSq = distSq;
            }
        }

        return nearest;
    }

    private void Fire(Vector2 direction)
    {
        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.Direction = direction;
        projectile.Speed = ProjectileSpeed;
        projectile.Damage = Damage;
        projectile.MaxDistance = Range * 1.25f;

        // Projectiles live in the world, not under the player, so they don't move with it.
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
    }
}
