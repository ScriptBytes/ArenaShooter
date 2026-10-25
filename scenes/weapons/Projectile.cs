using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>How a projectile is drawn. Purely visual.</summary>
public enum ProjectileShape
{
    Orb,
    /// <summary>A spinning blade (Throwing Axe).</summary>
    Blade,
    /// <summary>A pointed dart facing its flight (Arcane Missiles).</summary>
    Dart,
    /// <summary>A bat with flapping wings (Vampire Bats).</summary>
    Bat,
}

/// <summary>
/// A flying shot. By default it hits the first body it touches, or explodes in an area
/// (on hit or at max distance) when <see cref="ExplosionRadius"/> is set. Options:
/// <see cref="Pierce"/> flies through enemies (hitting each once), <see cref="ReturnTo"/> makes it
/// fly back to that node at the end of its path (hitting everything again), and
/// <see cref="Homing"/> steers it toward <see cref="HomingTarget"/> (retargeting if it dies).
/// </summary>
public partial class Projectile : Area2D
{
    private const float HomingRetargetRange = 320f;
    private const float CatchDistance = 24f;

    public Vector2 Direction { get; set; } = Vector2.Right;
    public float Speed { get; set; } = 600f;
    public float Damage { get; set; } = 1f;
    public float MaxDistance { get; set; } = 500f;
    public float Radius { get; set; } = 5f;
    public Color Color { get; set; } = new("e6f1ff");
    public float ExplosionRadius { get; set; }
    public HitEffects Effects { get; set; } = new();
    public ProjectileShape Shape { get; set; } = ProjectileShape.Orb;

    /// <summary>Flies through enemies instead of stopping at the first one. Walls still stop it.</summary>
    public bool Pierce { get; set; }

    /// <summary>When set, the projectile flies back to this node after <see cref="MaxDistance"/> and vanishes on reaching it.</summary>
    public Node2D ReturnTo { get; set; }

    /// <summary>Turn rate in radians per second toward <see cref="HomingTarget"/>; 0 = flies straight.</summary>
    public float Homing { get; set; }
    public Enemy HomingTarget { get; set; }

    /// <summary>Camera trauma added when an explosive projectile detonates.</summary>
    public float ScreenShake { get; set; }

    private float _traveled;
    private float _time;
    private bool _spent;
    private bool _returning;
    private Enemy _reservedOn;
    private readonly HashSet<Node> _pierced = new();

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        BodyEntered += Impact;
    }

    /// <summary>
    /// Marks <see cref="Damage"/> as incoming on <paramref name="enemy"/> so other abilities
    /// don't waste shots on it. Released when this projectile lands or leaves the tree.
    /// </summary>
    public void ReserveOn(Enemy enemy)
    {
        ReleaseReservation();
        _reservedOn = enemy;
        enemy.ReserveDamage(Damage);
    }

    public override void _ExitTree() => ReleaseReservation();

    private void ReleaseReservation()
    {
        if (IsInstanceValid(_reservedOn))
            _reservedOn.ReleaseDamage(Damage);
        _reservedOn = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        if (_returning)
        {
            if (!IsInstanceValid(ReturnTo))
            {
                QueueFree();
                return;
            }
            Direction = GlobalPosition.DirectionTo(ReturnTo.GlobalPosition);
            if (GlobalPosition.DistanceTo(ReturnTo.GlobalPosition) <= CatchDistance)
            {
                QueueFree();
                return;
            }
        }
        else if (Homing > 0f)
        {
            Steer(dt);
        }

        var step = Direction * Speed * dt;
        Position += step;
        _traveled += step.Length();
        if (Shape is ProjectileShape.Blade or ProjectileShape.Bat)
            QueueRedraw();

        if (!_returning && _traveled >= MaxDistance)
        {
            if (ReturnTo != null)
                StartReturn();
            else
                Impact(null);
        }
    }

    private void Steer(float dt)
    {
        if (!IsInstanceValid(HomingTarget) || HomingTarget.Health.IsDead)
        {
            HomingTarget = Combat.NearestEnemy(GetTree(), GlobalPosition, HomingRetargetRange);
            if (HomingTarget == null)
                return;
            ReserveOn(HomingTarget);
        }

        float angle = Direction.AngleTo(GlobalPosition.DirectionTo(HomingTarget.GlobalPosition));
        Direction = Direction.Rotated(Mathf.Clamp(angle, -Homing * dt, Homing * dt));
        Rotation = Direction.Angle();
    }

    // Turn around: everything can be hit once more on the way back.
    private void StartReturn()
    {
        _returning = true;
        _pierced.Clear();
    }

    private void Impact(Node2D body)
    {
        // Two bodies can enter on the same physics step; only resolve the first.
        if (_spent)
            return;
        // Enemy shots fly through minions that enemies ignore.
        if (body is Minion { Targetable: false })
            return;

        if (Pierce && body is Enemy pierced)
        {
            if (_pierced.Add(pierced))
                Combat.Hit(pierced, Damage, Effects, GlobalPosition);
            return;
        }
        // A returning blade bounces off walls instead of breaking.
        if (ReturnTo != null && body != null && body is not Enemy)
        {
            if (!_returning)
                StartReturn();
            return;
        }

        _spent = true;
        ReleaseReservation();

        if (ExplosionRadius > 0f)
        {
            Combat.DamageArea(GetTree(), GlobalPosition, ExplosionRadius, Damage, Effects);
            AreaFlash.Spawn(GetParent(), GlobalPosition, ExplosionRadius, Color);
            if (ScreenShake > 0f)
                ShakeCamera.Shake(GetTree(), ScreenShake);
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
        switch (Shape)
        {
            case ProjectileShape.Blade:
            {
                // Two crossed blades spinning fast, with a soft disc behind them.
                float spin = _time * 18f;
                DrawCircle(Vector2.Zero, Radius * 1.6f, new Color(Color, 0.25f));
                for (int i = 0; i < 2; i++)
                {
                    var along = Vector2.FromAngle(spin + Mathf.Pi / 2f * i) * Radius * 1.8f;
                    DrawLine(-along, along, Color, Radius * 0.7f);
                }
                break;
            }
            case ProjectileShape.Dart:
                // Drawn along +X; Rotation follows the flight (set while homing, and on spawn).
                DrawColoredPolygon([new Vector2(Radius * 2f, 0), new Vector2(-Radius, Radius), new Vector2(-Radius * 0.4f, 0), new Vector2(-Radius, -Radius)], Color);
                DrawCircle(Vector2.Zero, Radius * 1.4f, new Color(Color, 0.2f));
                break;
            case ProjectileShape.Bat:
            {
                // A small body with two wings that flap.
                float flap = Mathf.Sin(_time * 30f) * 0.5f + 0.5f;
                float wing = Radius * (1.2f + flap);
                DrawColoredPolygon([new Vector2(0, 0), new Vector2(-Radius * 0.6f, -wing), new Vector2(-Radius * 1.4f, -wing * 0.4f)], Color);
                DrawColoredPolygon([new Vector2(0, 0), new Vector2(-Radius * 0.6f, wing), new Vector2(-Radius * 1.4f, wing * 0.4f)], Color);
                DrawCircle(Vector2.Zero, Radius * 0.7f, Color.Darkened(0.3f));
                break;
            }
            default:
                DrawCircle(Vector2.Zero, Radius, Color);
                break;
        }
    }
}
