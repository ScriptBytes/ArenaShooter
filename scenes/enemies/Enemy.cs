using Godot;

namespace ArenaShooter;

/// <summary>
/// Generic enemy driven by <see cref="EnemyData"/>. Melee types chase their target; ranged
/// types hold at their preferred distance and shoot projectiles that hit the player and
/// targetable minions. The target is the nearest member of <see cref="Groups.EnemyTargets"/>
/// (the player, or a minion whose type is Targetable), re-picked a few times per second.
/// </summary>
public partial class Enemy : CharacterBody2D, IDamageable
{
    [Signal] public delegate void DiedEventHandler(Enemy enemy);

    private const float KnockbackDecay = 1800f;
    private const float HitPopTime = 0.12f;
    private const float DotTickInterval = 0.5f;
    private const uint ShotCollisionMask = 1 | 2 | 16; // world + player + minions
    private const float RetargetInterval = 0.25f;
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
    private float _retargetLeft;
    private float _speed;
    private float _shotDamage;
    private float _shotCooldown;
    private Vector2 _facing = Vector2.Right;
    private float _hitFlashLeft;
    private float _hitPop;
    private float _slowLeft;
    private float _slowMultiplier = 1f;
    private Vector2 _knockback;

    private float _dotDps;
    private float _dotLeft;
    private float _dotTick;
    private float _dotLifeSteal;
    private Color _dotColor;

    private MinionData _raiseMinion;
    private float _raiseChance;
    private float _raiseLeft;

    public override void _Ready()
    {
        AddToGroup(Groups.Enemies);
        Health = GetNode<Health>("Health");
        Health.Died += OnDied;
        // Staggered so spawns don't all retarget on one frame. Not random: seeded runs stay repeatable.
        _retargetLeft = GetInstanceId() % 8 / 8f * RetargetInterval;
        _target = FindTarget();

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

    /// <summary>
    /// Damage over time, ticking every half second. The strongest DoT wins; a weaker one
    /// doesn't replace or extend it. Ticks heal the player for <paramref name="lifeSteal"/> (0–1).
    /// </summary>
    public void ApplyDot(float dps, float duration, float lifeSteal, Color color)
    {
        if (_dotLeft > 0f && dps < _dotDps)
            return;
        if (_dotLeft <= 0f)
            _dotTick = DotTickInterval;

        _dotDps = dps;
        _dotLifeSteal = lifeSteal;
        _dotColor = color;
        _dotLeft = Mathf.Max(_dotLeft, duration);
    }

    /// <summary>If this enemy dies within <paramref name="window"/> seconds, it may rise as a minion.</summary>
    public void MarkForRaising(MinionData minion, float chance, float window)
    {
        if (_raiseLeft > 0f && chance < _raiseChance)
            return;

        _raiseMinion = minion;
        _raiseChance = chance;
        _raiseLeft = Mathf.Max(_raiseLeft, window);
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

        _retargetLeft -= dt;
        if (_retargetLeft <= 0f || !IsInstanceValid(_target) || _target.IsQueuedForDeletion())
        {
            _retargetLeft = RetargetInterval;
            _target = FindTarget();
        }

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
        _raiseLeft = Mathf.Max(_raiseLeft - dt, 0f);
        QueueRedraw();

        // Last, since a tick can kill.
        if (_dotLeft > 0f)
            TickDot(dt);
    }

    /// <summary>Nearest of the player and the targetable minions.</summary>
    private Node2D FindTarget()
    {
        Node2D nearest = null;
        float nearestDistSq = float.MaxValue;
        foreach (var node in GetTree().GetNodesInGroup(Groups.EnemyTargets))
        {
            if (node is not Node2D candidate || candidate.IsQueuedForDeletion())
                continue;

            float distSq = GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
            if (distSq < nearestDistSq)
            {
                nearest = candidate;
                nearestDistSq = distSq;
            }
        }
        return nearest;
    }

    private void TickDot(float dt)
    {
        _dotTick -= dt;
        if (_dotTick <= 0f)
        {
            _dotTick += DotTickInterval;
            Combat.Deal(this, _dotDps * DotTickInterval, _dotLifeSteal, color: _dotColor.Lightened(0.35f));
        }
        _dotLeft -= dt;
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

    /// <summary><paramref name="color"/> tints the damage number (DoT ticks); white by default.</summary>
    public void TakeDamage(float amount, bool crit, Color? color = null)
    {
        if (Health.IsDead || amount <= 0f)
            return;

        Health.TakeDamage(amount);
        _hitFlashLeft = 0.08f;
        _hitPop = 1f;
        FloatingText.SpawnDamage(GetTree().CurrentScene, GlobalPosition + new Vector2(0, -Data.Size / 2f), amount, color ?? Colors.White, crit);
    }

    private void OnDied()
    {
        // Deferred: we're usually inside a physics callback, where adding nodes is unsafe.
        var coin = new Coin { Value = Data.GoldValue, Position = GlobalPosition };
        GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, coin);
        if (GD.Randf() < Data.HeartDropChance)
        {
            var heart = new Heart { Position = GlobalPosition + new Vector2(10f, 6f) };
            GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, heart);
        }
        DeathPop.Spawn(GetTree().CurrentScene, GlobalPosition, Data.Color, Data.Size);
        if (_raiseLeft > 0f && GD.Randf() < _raiseChance)
            Minion.Spawn(GetTree().CurrentScene, _raiseMinion, GlobalPosition);

        EmitSignal(SignalName.Died, this);
        QueueFree();
    }

    public override void _Draw()
    {
        var color = Data.Color;
        if (_slowLeft > 0f)
            color = color.Lerp(new Color("7fc8ff"), 0.6f);
        if (_dotLeft > 0f)
            color = color.Lerp(_dotColor, 0.5f);
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
        // Marked for raising: a ring in the minion's glow color.
        if (_raiseLeft > 0f && _raiseMinion != null)
            DrawArc(Vector2.Zero, r + 5f, 0f, Mathf.Tau, 24, new Color(_raiseMinion.GlowColor, 0.8f), 2f);
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
