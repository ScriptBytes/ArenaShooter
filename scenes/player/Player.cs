using Godot;

namespace ArenaShooter;

public partial class Player : CharacterBody2D, IDamageable
{
    [Export] public float Speed { get; set; } = 300f;
    [Export] public float Radius { get; set; } = 16f;
    [Export] public float InvulnerabilityTime { get; set; } = 0.5f;

    public Health Health { get; private set; }
    public Camera2D Camera { get; private set; }

    private Area2D _hurtbox;
    private float _invulnerableLeft;

    public override void _Ready()
    {
        AddToGroup(Groups.Player);
        Health = GetNode<Health>("Health");
        Camera = GetNode<Camera2D>("Camera2D");
        _hurtbox = GetNode<Area2D>("Hurtbox");
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = input * Speed;
        MoveAndSlide();

        _invulnerableLeft -= (float)delta;
        if (_invulnerableLeft <= 0f)
            ApplyContactDamage();

        QueueRedraw();
    }

    public void TakeDamage(float amount)
    {
        if (_invulnerableLeft > 0f || Health.IsDead)
            return;

        Health.TakeDamage(amount);
        _invulnerableLeft = InvulnerabilityTime;
    }

    // Enemies don't physically block the player; touching one hurts instead.
    // Take one hit from the strongest enemy touching us, then i-frames prevent stacking.
    private void ApplyContactDamage()
    {
        float strongest = 0f;
        foreach (var body in _hurtbox.GetOverlappingBodies())
        {
            if (body is Enemy enemy)
                strongest = Mathf.Max(strongest, enemy.ContactDamage);
        }

        if (strongest > 0f)
            TakeDamage(strongest);
    }

    public override void _Draw()
    {
        bool blink = _invulnerableLeft > 0f && (int)(_invulnerableLeft * 20f) % 2 == 0;
        DrawCircle(Vector2.Zero, Radius, blink ? Colors.White : new Color("4aa3ff"));
    }
}
