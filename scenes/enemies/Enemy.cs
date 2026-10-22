using Godot;

namespace ArenaShooter;

public partial class Enemy : CharacterBody2D, IDamageable
{
    [Signal] public delegate void DiedEventHandler(Enemy enemy);

    private const float KnockbackDecay = 1800f;

    [Export] public float Speed { get; set; } = 90f;
    [Export] public float ContactDamage { get; set; } = 5f;
    [Export] public float Size { get; set; } = 24f;
    [Export] public int GoldValue { get; set; } = 1;

    public Health Health { get; private set; }

    private Node2D _target;
    private float _hitFlashLeft;
    private float _slowLeft;
    private float _slowMultiplier = 1f;
    private Vector2 _knockback;

    public override void _Ready()
    {
        AddToGroup(Groups.Enemies);
        Health = GetNode<Health>("Health");
        Health.Died += OnDied;
        _target = GetTree().GetFirstNodeInGroup(Groups.Player) as Node2D;
    }

    /// <summary>Applies wave difficulty. Call after the enemy is in the tree.</summary>
    public void ApplyWave(WaveSettings wave)
    {
        Health.SetMax(Health.Max * wave.EnemyHealthMultiplier, refill: true);
        ContactDamage += wave.EnemyDamageBonus;
        Speed *= wave.EnemySpeedMultiplier;
    }

    /// <summary>Slows movement to <paramref name="multiplier"/> of normal. Strongest slow wins.</summary>
    public void ApplySlow(float multiplier, float duration)
    {
        _slowMultiplier = _slowLeft > 0f ? Mathf.Min(_slowMultiplier, multiplier) : multiplier;
        _slowLeft = Mathf.Max(_slowLeft, duration);
    }

    public void ApplyKnockback(Vector2 impulse)
    {
        _knockback += impulse;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        float speedMultiplier = _slowLeft > 0f ? _slowMultiplier : 1f;

        var chase = IsInstanceValid(_target)
            ? GlobalPosition.DirectionTo(_target.GlobalPosition) * Speed * speedMultiplier
            : Vector2.Zero;
        Velocity = chase + _knockback;
        MoveAndSlide();

        _knockback = _knockback.MoveToward(Vector2.Zero, KnockbackDecay * dt);
        _slowLeft = Mathf.Max(_slowLeft - dt, 0f);
        _hitFlashLeft = Mathf.Max(_hitFlashLeft - dt, 0f);
        QueueRedraw();
    }

    public void TakeDamage(float amount)
    {
        Health.TakeDamage(amount);
        _hitFlashLeft = 0.08f;
    }

    private void OnDied()
    {
        // Deferred: we're usually inside a physics callback, where adding nodes is unsafe.
        var coin = new Coin { Value = GoldValue, Position = GlobalPosition };
        GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, coin);

        EmitSignal(SignalName.Died, this);
        QueueFree();
    }

    public override void _Draw()
    {
        var color = new Color("e5484d");
        if (_slowLeft > 0f)
            color = new Color("7fa8d8");
        if (_hitFlashLeft > 0f)
            color = Colors.White;

        DrawRect(new Rect2(-Size / 2f, -Size / 2f, Size, Size), color);
    }
}
