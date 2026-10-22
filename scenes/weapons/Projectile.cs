using Godot;

namespace ArenaShooter;

public partial class Projectile : Area2D
{
    [Export] public float Radius { get; set; } = 5f;

    public Vector2 Direction { get; set; } = Vector2.Right;
    public float Speed { get; set; } = 600f;
    public float Damage { get; set; } = 1f;
    public float MaxDistance { get; set; } = 500f;

    private float _traveled;
    private bool _spent;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        var step = Direction * Speed * (float)delta;
        Position += step;
        _traveled += step.Length();

        if (_traveled >= MaxDistance)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        // Two bodies can enter on the same physics step; only hit the first.
        if (_spent)
            return;
        _spent = true;

        if (body is IDamageable target)
            target.TakeDamage(Damage);

        QueueFree();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, new Color("ffd166"));
    }
}
