using System;
using Godot;

namespace ArenaShooter;

public partial class Player : CharacterBody2D, IDamageable
{
    [Signal] public delegate void GoldCollectedEventHandler(int amount);

    // Base value; ApplyStats layers run bonuses on top. Other base stats come from ClassData.
    [Export] public float PickupRange { get; set; } = 80f;
    [Export] public float Radius { get; set; } = 16f;
    [Export] public float InvulnerabilityTime { get; set; } = 0.5f;

    public float Speed { get; private set; } = 300f;

    /// <summary>Flat reduction to each hit taken (minimum 1 damage).</summary>
    public float Armor { get; private set; }

    public Health Health { get; private set; }
    public Camera2D Camera { get; private set; }
    public AbilityCaster Abilities { get; private set; }

    private ClassData _class;
    private Area2D _hurtbox;
    private float _invulnerableLeft;
    private float _basePickupRange;

    private Vector2 _leapVelocity;
    private float _leapLeft;
    private float _leapDuration;
    private Action _onLanded;

    public bool IsLeaping => _leapLeft > 0f;

    public override void _Ready()
    {
        AddToGroup(Groups.Player);
        Health = GetNode<Health>("Health");
        Camera = GetNode<Camera2D>("Camera2D");
        Abilities = GetNode<AbilityCaster>("Abilities");
        _hurtbox = GetNode<Area2D>("Hurtbox");
        _basePickupRange = PickupRange;
    }

    public void Setup(ClassData playerClass)
    {
        _class = playerClass;
        Abilities.Setup(playerClass.Weapon, playerClass.Spells);
    }

    public void ApplyStats(PlayerStats stats)
    {
        Speed = _class.MoveSpeed * stats.Multiplier(StatType.MoveSpeed, floor: 0.3f);
        PickupRange = Mathf.Max(_basePickupRange + stats.Get(StatType.PickupRange), 0f);
        Armor = _class.Armor + stats.Get(StatType.Armor);
        Health.SetMax(_class.MaxHealth + stats.Get(StatType.MaxHealth), refill: false);
        Abilities.ApplyStats(stats);
    }

    public void CollectGold(int amount) => EmitSignal(SignalName.GoldCollected, amount);

    /// <summary>
    /// Jumps to <paramref name="target"/> over <paramref name="duration"/> seconds, untouchable
    /// while airborne, then calls <paramref name="onLanded"/>. Walls cut the leap short.
    /// </summary>
    public void Leap(Vector2 target, float duration, Action onLanded)
    {
        _leapDuration = _leapLeft = duration;
        _leapVelocity = (target - GlobalPosition) / duration;
        _onLanded = onLanded;
        _invulnerableLeft = Mathf.Max(_invulnerableLeft, duration + 0.15f);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _invulnerableLeft -= dt;
        QueueRedraw();

        if (IsLeaping)
        {
            Velocity = _leapVelocity;
            MoveAndSlide();

            _leapLeft -= dt;
            if (!IsLeaping)
            {
                var onLanded = _onLanded;
                _onLanded = null;
                onLanded?.Invoke();
            }
            return;
        }

        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = input * Speed;
        MoveAndSlide();

        if (_invulnerableLeft <= 0f)
            ApplyContactDamage();
    }

    public void TakeDamage(float amount)
    {
        if (_invulnerableLeft > 0f || Health.IsDead)
            return;

        Health.TakeDamage(Mathf.Max(amount - Armor, 1f));
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
        var color = _class?.Color ?? new Color("4aa3ff");

        if (IsLeaping)
        {
            // Fake height: a shadow stays on the ground while the body swells mid-air.
            float t = 1f - _leapLeft / _leapDuration;
            float lift = Mathf.Sin(Mathf.Pi * t);
            DrawCircle(Vector2.Zero, Radius * 0.9f, new Color(0, 0, 0, 0.35f));
            DrawCircle(new Vector2(0, -12f * lift), Radius * (1f + 0.35f * lift), color);
            return;
        }

        bool blink = _invulnerableLeft > 0f && (int)(_invulnerableLeft * 20f) % 2 == 0;
        DrawCircle(Vector2.Zero, Radius, blink ? Colors.White : color);
    }
}
