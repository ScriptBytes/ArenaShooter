using Godot;

namespace ArenaShooter;

/// <summary>
/// A <see cref="DrainAbility"/>'s beam while it lasts: ticks damage on its target, retargets
/// when the target dies or leaves range, and draws the tether with motes flowing back to the caster.
/// Lives in the world at the origin and draws in global coordinates.
/// </summary>
public partial class DrainBeam : Node2D
{
    private const int Motes = 4;

    private AbilityContext _ctx;
    private DrainAbility _ability;
    private HitEffects _effects;
    private Enemy _target;
    private float _timeLeft;
    private float _tickLeft;
    private float _time;

    public static void Spawn(Node world, AbilityContext ctx, DrainAbility ability, Enemy target, HitEffects effects)
    {
        var beam = new DrainBeam
        {
            _ctx = ctx,
            _ability = ability,
            _effects = effects,
            _target = target,
            _timeLeft = ability.Duration,
        };
        world.CallDeferred(Node.MethodName.AddChild, beam);
    }

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        _timeLeft -= dt;
        if (_timeLeft <= 0f || !IsInstanceValid(_ctx.Caster))
        {
            QueueFree();
            return;
        }

        // Some slack before letting go, so the beam doesn't flicker at the edge of range.
        float range = _ability.EffectiveRange(_ctx);
        if (!IsInstanceValid(_target) || _target.Health.IsDead
            || _ctx.Origin.DistanceTo(_target.GlobalPosition) > range * 1.25f)
        {
            _target = Combat.NearestEnemy(_ctx.Tree, _ctx.Origin, range);
            if (_target == null)
            {
                QueueFree();
                return;
            }
        }

        QueueRedraw();
        _tickLeft -= dt;
        if (_tickLeft > 0f)
            return;

        _tickLeft += _ability.TickInterval;
        Combat.Hit(_target, _ability.Damage * _ctx.DamageMultiplier * _ability.TickInterval, _effects, _ctx.Origin);
    }

    public override void _Draw()
    {
        if (!IsInstanceValid(_target) || !IsInstanceValid(_ctx.Caster))
            return;

        var from = ToLocal(_ctx.Origin);
        var to = ToLocal(_target.GlobalPosition);
        var side = (to - from).Orthogonal().Normalized();
        var color = _ability.IconColor;

        // A gently writhing line: a sine wave whose amplitude is zero at both ends.
        const int segments = 14;
        var points = new Vector2[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float wave = Mathf.Sin(t * Mathf.Pi * 3f - _time * 14f) * 6f * Mathf.Sin(t * Mathf.Pi);
            points[i] = from.Lerp(to, t) + side * wave;
        }
        DrawPolyline(points, new Color(color, 0.3f), 9f);
        DrawPolyline(points, new Color(color, 0.9f), 3f);

        // Motes travel from the target back to the caster: the life being drained.
        for (int i = 0; i < Motes; i++)
        {
            float t = Mathf.PosMod(_time * 1.6f + (float)i / Motes, 1f);
            DrawCircle(to.Lerp(from, t), 3.5f, new Color(color.Lightened(0.4f), 0.9f));
        }
        DrawCircle(to, 6f + 2f * Mathf.Sin(_time * 20f), new Color(color, 0.5f));
    }
}
