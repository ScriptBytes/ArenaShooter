using Godot;

namespace ArenaShooter;

public partial class Enemy : CharacterBody2D, IDamageable
{
    [Signal] public delegate void DiedEventHandler(Enemy enemy);

    [Export] public float Speed { get; set; } = 90f;
    [Export] public float ContactDamage { get; set; } = 5f;
    [Export] public float Size { get; set; } = 24f;
    [Export] public int GoldValue { get; set; } = 1;

    public Health Health { get; private set; }

    private Node2D _target;
    private float _hitFlashLeft;

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

    public override void _PhysicsProcess(double delta)
    {
        if (IsInstanceValid(_target))
        {
            Velocity = GlobalPosition.DirectionTo(_target.GlobalPosition) * Speed;
            MoveAndSlide();
        }

        if (_hitFlashLeft > 0f)
        {
            _hitFlashLeft -= (float)delta;
            QueueRedraw();
        }
    }

    public void TakeDamage(float amount)
    {
        Health.TakeDamage(amount);
        _hitFlashLeft = 0.08f;
        QueueRedraw();
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
        var color = _hitFlashLeft > 0f ? Colors.White : new Color("e5484d");
        DrawRect(new Rect2(-Size / 2f, -Size / 2f, Size, Size), color);
    }
}
