using Godot;

namespace ArenaShooter;

/// <summary>
/// An allied unit (skeletons, golems). Chases the nearest enemy close to the player and attacks
/// it, or follows the player when there's nothing to fight. A <see cref="MinionData.Targetable"/>
/// type joins <see cref="Groups.EnemyTargets"/>: enemies may chase it instead of the player, their
/// contact hurts it and enemy shots hit it, so it tanks. Otherwise enemies ignore it and it can't be
/// hurt. Either way it expires after <see cref="MinionData.Lifetime"/>. Physics layer 5 (minions):
/// it bumps into walls and each other, not into enemies or the player. Built in code, like pickups.
/// </summary>
public partial class Minion : CharacterBody2D, IDamageable
{
    private const uint MinionLayer = 16;
    private const uint WorldLayer = 1;
    private const float LeashRange = 420f;
    private const float FollowDistance = 70f;
    private const float RetargetInterval = 0.2f;
    private const float InvulnerabilityTime = 0.5f;
    private const float FadeTime = 1.5f;

    private static ulong _spawnCounter;

    public MinionData Data { get; set; }

    /// <summary>
    /// Scales every minion's <see cref="MinionData.MaxHealth"/> for the current wave
    /// (<see cref="WaveSettings.MinionHealthMultiplier"/>). Set by Main at each wave start; minions
    /// spawned during the wave read it once, in _Ready.
    /// </summary>
    public static float WaveHealthMultiplier { get; set; } = 1f;

    private float _maxHealth;

    /// <summary>Spawn order, so the oldest one crumbles first when over the cap.</summary>
    public ulong SpawnOrder { get; private set; }

    /// <summary>Enemies may chase, touch and shoot it (see <see cref="MinionData.Targetable"/>).</summary>
    public bool Targetable => Data.Targetable;

    private Player _player;
    private Enemy _target;
    private float _health;
    private float _lifeLeft;
    private float _attackLeft;
    private float _retargetLeft;
    private float _hurtLeft;
    private float _hitFlashLeft;
    private float _lunge;
    private Vector2 _facing = Vector2.Right;
    private Vector2 _followOffset;

    /// <summary>Adds a minion at <paramref name="position"/>. Deferred, so it's safe from physics callbacks.</summary>
    public static void Spawn(Node world, MinionData data, Vector2 position)
    {
        var minion = new Minion { Data = data, Position = position };
        world.CallDeferred(Node.MethodName.AddChild, minion);
    }

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        AddToGroup(Groups.Minions);
        if (Targetable)
            AddToGroup(Groups.EnemyTargets);
        SpawnOrder = ++_spawnCounter;
        _player = GetTree().GetFirstNodeInGroup(Groups.Player) as Player;

        CollisionLayer = MinionLayer;
        CollisionMask = WorldLayer | MinionLayer;
        MotionMode = MotionModeEnum.Floating;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Data.Size / 2f } });

        _maxHealth = Data.MaxHealth * WaveHealthMultiplier;
        _health = _maxHealth;
        _lifeLeft = Data.Lifetime;
        _attackLeft = Data.AttackCooldown * 0.5f;
        _followOffset = Vector2.FromAngle(SpawnOrder * 2.4f) * FollowDistance;

        AreaFlash.Spawn(GetParent(), GlobalPosition, Data.Size * 1.4f, Data.GlowColor);
        EnforceCap();
    }

    // Over the cap: crumble the oldest of this type. Runs once this one is in the group.
    private void EnforceCap()
    {
        Minion oldest = null;
        int alive = 0;
        foreach (var node in GetTree().GetNodesInGroup(Groups.Minions))
        {
            if (node is not Minion other || other.Data != Data || other.IsQueuedForDeletion())
                continue;
            alive++;
            if (oldest == null || other.SpawnOrder < oldest.SpawnOrder)
                oldest = other;
        }

        if (alive > Data.MaxAlive)
            oldest?.Crumble();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _lifeLeft -= dt;
        if (_lifeLeft <= 0f)
        {
            Crumble();
            return;
        }

        _attackLeft -= dt;
        _hurtLeft -= dt;
        _hitFlashLeft = Mathf.Max(_hitFlashLeft - dt, 0f);
        _lunge = Mathf.Max(_lunge - dt * 6f, 0f);
        _retargetLeft -= dt;
        if (_retargetLeft <= 0f || !IsInstanceValid(_target) || _target.Health.IsDead)
        {
            _retargetLeft = RetargetInterval;
            _target = FindTarget();
        }

        if (_target != null)
            Fight(_target);
        else
            Follow();

        MoveAndSlide();
        TakeContactDamage();
        QueueRedraw();
    }

    /// <summary>Nearest living enemy that isn't too far from the player, so minions stay close.</summary>
    private Enemy FindTarget()
    {
        if (_player == null)
            return null;

        Enemy nearest = null;
        float nearestDistSq = float.MaxValue;
        float leashSq = LeashRange * LeashRange;
        foreach (var node in GetTree().GetNodesInGroup(Groups.Enemies))
        {
            if (node is not Enemy enemy || enemy.Health.IsDead
                || _player.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition) > leashSq)
                continue;

            float distSq = GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (distSq < nearestDistSq)
            {
                nearest = enemy;
                nearestDistSq = distSq;
            }
        }
        return nearest;
    }

    private void Fight(Enemy enemy)
    {
        var toEnemy = enemy.GlobalPosition - GlobalPosition;
        float gap = toEnemy.Length() - Data.Size / 2f - enemy.Data.Size / 2f;
        if (toEnemy != Vector2.Zero)
            _facing = toEnemy.Normalized();

        Velocity = gap > Data.AttackReach ? _facing * Data.Speed : Vector2.Zero;
        if (gap > Data.AttackReach || _attackLeft > 0f)
            return;

        _attackLeft = Data.AttackCooldown;
        _lunge = 1f;

        var ctx = _player.Abilities.Context;
        float damage = Data.Damage * ctx.MinionDamageMultiplier;
        // Minions crit like a default ability (5% + Crit Chance), and don't life steal.
        var effects = new HitEffects(Knockback: Data.Knockback,
            CritChance: Mathf.Clamp((5f + ctx.CritChanceBonus) / 100f, 0f, 1f), CritMultiplier: 1.5f,
            Source: Data.DisplayName);

        if (Data.IsRanged)
        {
            Shoot(enemy, damage, effects);
        }
        else if (Data.SplashRadius > 0f)
        {
            var center = enemy.GlobalPosition;
            Combat.DamageArea(GetTree(), center, Data.SplashRadius, damage, effects);
            AreaFlash.Spawn(GetParent(), center, Data.SplashRadius, Data.GlowColor);
        }
        else
        {
            Combat.Hit(enemy, damage, effects, GlobalPosition);
        }
    }

    private void Shoot(Enemy enemy, float damage, HitEffects effects)
    {
        var shot = Data.ProjectileScene.Instantiate<Projectile>();
        shot.Direction = _facing;
        shot.Rotation = _facing.Angle();
        shot.Speed = Data.ShotSpeed;
        shot.Damage = damage;
        shot.Radius = 5f;
        shot.Color = Data.GlowColor;
        shot.Shape = ProjectileShape.Dart;
        shot.Effects = effects;
        shot.MaxDistance = (Data.AttackReach + Data.Size + enemy.Data.Size) * 1.4f;
        GetParent().AddChild(shot);
        shot.GlobalPosition = GlobalPosition + _facing * Data.Size / 2f;
        shot.ReserveOn(enemy);
    }

    /// <summary>Corpse Explosion: bursts for <paramref name="damage"/> around itself and is gone.</summary>
    public void Detonate(float damage, float radius, HitEffects effects)
    {
        if (IsQueuedForDeletion())
            return;
        Combat.DamageArea(GetTree(), GlobalPosition, radius, damage, effects);
        AreaFlash.Spawn(GetParent(), GlobalPosition, radius, Data.GlowColor);
        Crumble();
    }

    private void Follow()
    {
        if (_player == null)
        {
            Velocity = Vector2.Zero;
            return;
        }

        var spot = _player.GlobalPosition + _followOffset;
        var toSpot = spot - GlobalPosition;
        Velocity = toSpot.Length() > 12f ? toSpot.Normalized() * Data.Speed : Vector2.Zero;
        if (Velocity != Vector2.Zero)
            _facing = Velocity.Normalized();
    }

    // Like the player's hurtbox, but by distance: one hit from the strongest enemy touching it.
    private void TakeContactDamage()
    {
        if (!Targetable || _hurtLeft > 0f)
            return;

        float strongest = 0f;
        foreach (var enemy in Combat.EnemiesInRadius(GetTree(), GlobalPosition, Data.Size / 2f + 14f))
            strongest = Mathf.Max(strongest, enemy.ContactDamage);
        if (strongest <= 0f)
            return;

        TakeDamage(strongest);
        _hurtLeft = InvulnerabilityTime;
    }

    /// <summary>A hit from an enemy (contact or shot). Armor reduces it, minimum 1.</summary>
    public void TakeDamage(float amount)
    {
        if (!Targetable || IsQueuedForDeletion())
            return;

        _health -= Mathf.Max(amount - Data.Armor, 1f);
        _hitFlashLeft = 0.08f;
        if (_health <= 0f)
            Crumble();
    }

    private void Crumble()
    {
        if (IsQueuedForDeletion())
            return;
        DeathPop.Spawn(GetParent(), GlobalPosition, Data.Color, Data.Size);
        QueueFree();
    }

    public override void _Draw()
    {
        float r = Data.Size / 2f;
        float alpha = Mathf.Clamp(_lifeLeft / FadeTime, 0.25f, 1f);
        var body = _hitFlashLeft > 0f ? Colors.White : Data.Color;
        var offset = _facing * 4f * _lunge;

        // Bone-white body with a glowing outline, so allies read apart from the colored enemies.
        DrawCircle(offset, r, new Color(body, alpha));
        DrawArc(offset, r + 1.5f, 0f, Mathf.Tau, 24, new Color(Data.GlowColor, alpha), 2.5f);

        // Two dark eye sockets looking where it's heading.
        var side = _facing.Orthogonal() * r * 0.35f;
        var eyes = offset + _facing * r * 0.3f;
        var socket = new Color(0.1f, 0.1f, 0.12f, alpha);
        DrawCircle(eyes + side, r * 0.18f, socket);
        DrawCircle(eyes - side, r * 0.18f, socket);

        // Ranged types hold a glowing orb out in front: the staff they shoot from.
        if (Data.IsRanged)
            DrawCircle(offset + _facing * (r + 4f) + side * 1.4f, 3.5f + 1.5f * _lunge, new Color(Data.GlowColor.Lightened(0.3f), alpha));

        if (_health < _maxHealth)
        {
            var bar = new Rect2(-r, -r - 8f, r * 2f, 3f);
            DrawRect(bar.Grow(1f), new Color(0f, 0f, 0f, 0.7f * alpha));
            DrawRect(bar with { Size = new Vector2(bar.Size.X * _health / _maxHealth, bar.Size.Y) },
                new Color(Data.GlowColor, alpha));
        }
    }
}
