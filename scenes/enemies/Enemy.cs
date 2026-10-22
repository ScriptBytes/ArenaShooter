using Godot;

namespace ArenaShooter;

/// <summary>
/// Generic enemy driven by <see cref="EnemyData"/>. Melee types chase the player; ranged
/// types hold at their preferred distance and shoot projectiles that hit the player.
/// </summary>
public partial class Enemy : CharacterBody2D, IDamageable
{
    [Signal] public delegate void DiedEventHandler(Enemy enemy);

    private const float KnockbackDecay = 1800f;
    private const float HitPopTime = 0.12f;
    private const uint ShotCollisionMask = 1 | 2; // world + player
    private static readonly Color ShotColor = new("ff6b6b");

    /// <summary>Set before the enemy enters the tree (Enemy.tscn defaults to the grunt).</summary>
    [Export] public EnemyData Data { get; set; }
    [Export] public PackedScene ProjectileScene { get; set; }

    public Health Health { get; private set; }
    public float ContactDamage { get; private set; }

    /// <summary>Damage from projectiles already flying at this enemy (see <see cref="Projectile.ReserveOn"/>).</summary>
    public float IncomingDamage { get; private set; }

    /// <summary>Dead, or will be once the damage in flight lands. Auto-targeting skips these.</summary>
    public bool IsDoomed => Health.Current <= IncomingDamage + 0.001f;

    private Node2D _target;
    private float _speed;
    private float _shotDamage;
    private float _shotCooldown;
    private Vector2 _facing = Vector2.Right;
    private float _hitFlashLeft;
    private float _hitPop;
    private float _slowLeft;
    private float _slowMultiplier = 1f;
    private Vector2 _knockback;

    public override void _Ready()
    {
        AddToGroup(Groups.Enemies);
        Health = GetNode<Health>("Health");
        Health.Died += OnDied;
        _target = GetTree().GetFirstNodeInGroup(Groups.Player) as Node2D;

        // Each enemy gets its own shape; editing the scene's shared shape would resize them all.
        GetNode<CollisionShape2D>("CollisionShape2D").Shape = new CircleShape2D { Radius = Data.Size / 2f };
        Health.SetMax(Data.MaxHealth, refill: true);
        _speed = Data.Speed;
        ContactDamage = Data.ContactDamage;
        _shotDamage = Data.ShotDamage;
        // Stagger the first shot so a group of archers doesn't fire in unison.
        _shotCooldown = Data.ShotInterval * (float)GD.RandRange(0.5, 1.0);
    }

    /// <summary>Applies wave difficulty. Call after the enemy is in the tree.</summary>
    public void ApplyWave(WaveSettings wave)
    {
        Health.SetMax(Health.Max * wave.EnemyHealthMultiplier, refill: true);
        ContactDamage += wave.EnemyDamageBonus;
        _shotDamage += wave.EnemyDamageBonus;
        _speed *= wave.EnemySpeedMultiplier;
    }

    /// <summary>Slows movement to <paramref name="multiplier"/> of normal. Strongest slow wins.</summary>
    public void ApplySlow(float multiplier, float duration)
    {
        _slowMultiplier = _slowLeft > 0f ? Mathf.Min(_slowMultiplier, multiplier) : multiplier;
        _slowLeft = Mathf.Max(_slowLeft, duration);
    }

    public void ReserveDamage(float amount) => IncomingDamage += amount;

    public void ReleaseDamage(float amount) => IncomingDamage = Mathf.Max(IncomingDamage - amount, 0f);

    public void ApplyKnockback(Vector2 impulse)
    {
        _knockback += impulse * (1f - Data.KnockbackResistance);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (IsInstanceValid(_target))
        {
            var toTarget = _target.GlobalPosition - GlobalPosition;
            Move(toTarget);
            if (Data.IsRanged)
                UpdateShooting(toTarget, dt);
        }

        _knockback = _knockback.MoveToward(Vector2.Zero, KnockbackDecay * dt);
        _slowLeft = Mathf.Max(_slowLeft - dt, 0f);
        _hitFlashLeft = Mathf.Max(_hitFlashLeft - dt, 0f);
        _hitPop = Mathf.Max(_hitPop - dt / HitPopTime, 0f);
        QueueRedraw();
    }

    private void Move(Vector2 toTarget)
    {
        float distance = toTarget.Length();
        var direction = toTarget.Normalized();
        _facing = direction;

        // Ranged enemies close in until near their preferred distance, back off if crowded.
        if (Data.PreferredDistance > 0f)
        {
            if (distance < Data.PreferredDistance - 40f)
                direction = -direction;
            else if (distance <= Data.PreferredDistance + 20f)
                direction = Vector2.Zero;
        }

        float speedMultiplier = _slowLeft > 0f ? _slowMultiplier : 1f;
        Velocity = direction * _speed * speedMultiplier + _knockback;
        MoveAndSlide();
    }

    private void UpdateShooting(Vector2 toTarget, float dt)
    {
        _shotCooldown -= dt;
        if (_shotCooldown > 0f || toTarget.Length() > Data.ShotRange)
            return;

        _shotCooldown = Data.ShotInterval;

        var shot = ProjectileScene.Instantiate<Projectile>();
        shot.CollisionMask = ShotCollisionMask;
        shot.Direction = toTarget.Normalized();
        shot.Speed = Data.ShotSpeed;
        shot.Damage = _shotDamage;
        shot.Radius = 6f;
        shot.Color = ShotColor;
        shot.MaxDistance = Data.ShotRange * 1.3f;

        GetTree().CurrentScene.AddChild(shot);
        shot.GlobalPosition = GlobalPosition;
    }

    public void TakeDamage(float amount) => TakeDamage(amount, crit: false);

    public void TakeDamage(float amount, bool crit)
    {
        if (Health.IsDead || amount <= 0f)
            return;

        Health.TakeDamage(amount);
        _hitFlashLeft = 0.08f;
        _hitPop = 1f;
        DamageNumber.Spawn(GetTree().CurrentScene, GlobalPosition + new Vector2(0, -Data.Size / 2f), amount, Colors.White, crit);
    }

    private void OnDied()
    {
        // Deferred: we're usually inside a physics callback, where adding nodes is unsafe.
        var coin = new Coin { Value = Data.GoldValue, Position = GlobalPosition };
        GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, coin);
        DeathPop.Spawn(GetTree().CurrentScene, GlobalPosition, Data.Color, Data.Size);

        EmitSignal(SignalName.Died, this);
        QueueFree();
    }

    public override void _Draw()
    {
        var color = Data.Color;
        if (_slowLeft > 0f)
            color = color.Lerp(new Color("7fc8ff"), 0.6f);
        if (_hitFlashLeft > 0f)
            color = Colors.White;

        float r = Data.Size / 2f;
        // Swells briefly when hit, so each hit reads even in a crowd.
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One * (1f + 0.25f * _hitPop));
        switch (Data.Shape)
        {
            case EnemyShape.Triangle:
                // Points where it's heading, so fast runners read as "incoming".
                DrawColoredPolygon(
                    [_facing * r * 1.2f, _facing.Rotated(2.4f) * r, _facing.Rotated(-2.4f) * r], color);
                break;
            case EnemyShape.Diamond:
                DrawColoredPolygon(
                    [new Vector2(0, -r * 1.2f), new Vector2(r, 0), new Vector2(0, r * 1.2f), new Vector2(-r, 0)], color);
                break;
            case EnemyShape.Hexagon:
                var points = new Vector2[6];
                for (int i = 0; i < 6; i++)
                    points[i] = Vector2.FromAngle(Mathf.Tau * i / 6f) * r;
                DrawColoredPolygon(points, color);
                break;
            default:
                DrawRect(new Rect2(-r, -r, r * 2f, r * 2f), color);
                break;
        }

        DrawSetTransform(Vector2.Zero);
        if (Data.ShowHealthBar && Health.Current < Health.Max)
            DrawHealthBar(r);
    }

    private void DrawHealthBar(float r)
    {
        var bar = new Rect2(-r, -r * 1.2f - 10f, r * 2f, 4f);
        DrawRect(bar.Grow(1f), new Color(0f, 0f, 0f, 0.7f));
        DrawRect(bar with { Size = new Vector2(bar.Size.X * Health.Current / Health.Max, bar.Size.Y) }, new Color("e5484d"));
    }
}
