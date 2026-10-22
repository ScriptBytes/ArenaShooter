using Godot;

namespace ArenaShooter;

public partial class Player : CharacterBody2D, IDamageable
{
    [Signal] public delegate void GoldCollectedEventHandler(int amount);

    // Exported values are base stats; ApplyStats layers run bonuses on top.
    [Export] public float Speed { get; set; } = 300f;
    [Export] public float PickupRange { get; set; } = 80f;
    [Export] public float Radius { get; set; } = 16f;
    [Export] public float InvulnerabilityTime { get; set; } = 0.5f;

    /// <summary>Flat reduction to each hit taken (minimum 1 damage).</summary>
    public float Armor { get; private set; }

    public Health Health { get; private set; }
    public Camera2D Camera { get; private set; }

    private Weapon _weapon;
    private Area2D _hurtbox;
    private float _invulnerableLeft;

    private float _baseSpeed;
    private float _basePickupRange;
    private float _baseMaxHealth;

    public override void _Ready()
    {
        AddToGroup(Groups.Player);
        Health = GetNode<Health>("Health");
        Camera = GetNode<Camera2D>("Camera2D");
        _weapon = GetNode<Weapon>("Weapon");
        _hurtbox = GetNode<Area2D>("Hurtbox");

        _baseSpeed = Speed;
        _basePickupRange = PickupRange;
        _baseMaxHealth = Health.Max;
    }

    public void ApplyStats(PlayerStats stats)
    {
        Speed = _baseSpeed * stats.Multiplier(StatType.MoveSpeed, floor: 0.3f);
        PickupRange = Mathf.Max(_basePickupRange + stats.Get(StatType.PickupRange), 0f);
        Armor = stats.Get(StatType.Armor);
        Health.SetMax(_baseMaxHealth + stats.Get(StatType.MaxHealth), refill: false);
        _weapon.ApplyStats(stats);
    }

    public void CollectGold(int amount) => EmitSignal(SignalName.GoldCollected, amount);

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
        bool blink = _invulnerableLeft > 0f && (int)(_invulnerableLeft * 20f) % 2 == 0;
        DrawCircle(Vector2.Zero, Radius, blink ? Colors.White : new Color("4aa3ff"));
    }
}
