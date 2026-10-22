using Godot;

namespace ArenaShooter;

/// <summary>
/// Straight-flying projectile. Hits the first body it touches, or explodes in an area
/// (on hit or at max distance) when <see cref="ExplosionRadius"/> is set.
/// </summary>
public partial class Projectile : Area2D
{
    public Vector2 Direction { get; set; } = Vector2.Right;
    public float Speed { get; set; } = 600f;
    public float Damage { get; set; } = 1f;
    public float MaxDistance { get; set; } = 500f;
    public float Radius { get; set; } = 5f;
    public Color Color { get; set; } = new("e6f1ff");
    public float ExplosionRadius { get; set; }
    public HitEffects Effects { get; set; } = new();

    private float _traveled;
    private bool _spent;

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        BodyEntered += Impact;
    }

    public override void _PhysicsProcess(double delta)
    {
        var step = Direction * Speed * (float)delta;
        Position += step;
        _traveled += step.Length();

        if (_traveled >= MaxDistance)
            Impact(null);
    }

    private void Impact(Node2D body)
    {
        // Two bodies can enter on the same physics step; only resolve the first.
        if (_spent)
            return;
        _spent = true;

        if (ExplosionRadius > 0f)
        {
            Combat.DamageArea(GetTree(), GlobalPosition, ExplosionRadius, Damage, Effects);
            AreaFlash.Spawn(GetParent(), GlobalPosition, ExplosionRadius, Color);
        }
        else if (body is Enemy enemy)
        {
            Combat.Hit(enemy, Damage, Effects, GlobalPosition);
        }
        else if (body is IDamageable target)
        {
            target.TakeDamage(Damage);
        }

        QueueFree();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, Color);
    }
}
