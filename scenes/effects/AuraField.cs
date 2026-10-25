using Godot;

namespace ArenaShooter;

/// <summary>
/// An <see cref="AuraAbility"/> while it lasts: a circle that damages every enemy inside on each
/// tick, either following the caster or fixed on the ground. Draws a faint disc, a turning dashed
/// rim and motes (drawn inward to the center when it steals life, drifting down otherwise).
/// In <see cref="Groups.Transient"/>, so a wave ending clears it.
/// </summary>
public partial class AuraField : Node2D
{
    private const int Dashes = 12;
    private const int Motes = 10;

    private AbilityContext _ctx;
    private AuraAbility _ability;
    private HitEffects _effects;
    private float _damagePerTick;
    private float _timeLeft;
    private float _tickLeft;
    private float _time;

    public static void Spawn(Node world, AbilityContext ctx, AuraAbility ability, Vector2 center, HitEffects effects)
    {
        var field = new AuraField
        {
            _ctx = ctx,
            _ability = ability,
            _effects = effects,
            _damagePerTick = ability.Damage * ctx.DamageMultiplier * ability.TickInterval,
            _timeLeft = ability.Duration,
            _tickLeft = 0f,
            Position = center,
        };
        world.CallDeferred(Node.MethodName.AddChild, field);
    }

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        ZIndex = -1; // under the enemies inside it
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

        if (_ability.CenteredOnCaster)
            GlobalPosition = _ctx.Origin;
        QueueRedraw();

        _tickLeft -= dt;
        if (_tickLeft > 0f)
            return;
        _tickLeft += _ability.TickInterval;
        Combat.DamageArea(_ctx.Tree, GlobalPosition, _ability.Radius, _damagePerTick, _effects);
    }

    public override void _Draw()
    {
        var color = _ability.IconColor;
        float r = _ability.Radius;
        // Fades in quickly and out over the last half second.
        float alpha = Mathf.Clamp(Mathf.Min(_time * 6f, _timeLeft * 2f), 0f, 1f);

        DrawCircle(Vector2.Zero, r, new Color(color, 0.14f * alpha));
        float turn = _time * 1.5f;
        float dash = Mathf.Tau / Dashes;
        for (int i = 0; i < Dashes; i++)
            DrawArc(Vector2.Zero, r, turn + dash * i, turn + dash * (i + 0.55f), 6, new Color(color, 0.8f * alpha), 2.5f);

        bool drains = _ability.LifeSteal > 0f;
        for (int i = 0; i < Motes; i++)
        {
            float t = Mathf.PosMod(_time * 0.8f + (float)i / Motes, 1f);
            float angle = i * 2.4f;
            var at = drains
                ? Vector2.FromAngle(angle) * r * (1f - t)                                   // pulled to the center
                : Vector2.FromAngle(angle) * r * 0.85f * Mathf.Sqrt((i + 1f) / Motes) * new Vector2(1f, 0f)
                  + new Vector2(0, -r * 0.8f + r * 1.6f * t);                                // falling through
            if (!drains && at.Length() > r)
                continue;
            DrawCircle(at, 3f, new Color(color.Lightened(0.4f), 0.85f * alpha * (drains ? t : 1f)));
        }
    }
}
