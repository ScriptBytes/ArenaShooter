using Godot;

namespace ArenaShooter;

/// <summary>
/// Gives the caster timed stat bonuses (War Cry, Shield Wall), plus an optional blast around
/// them: set Damage to 0 for a pure shove. Auto-casts once <see cref="AreaAbility.MinTargets"/>
/// enemies are within the radius, so the buff isn't wasted on an empty arena.
/// A <see cref="HealthCost"/> makes it a sacrifice (Blood Pact): auto-cast then also waits
/// until paying it would still leave the caster above half health.
/// </summary>
[GlobalClass]
public partial class BuffAbility : AreaAbility
{
    [ExportGroup("Buff")]
    [Export] public float Duration { get; set; } = 5f;
    [Export] public Godot.Collections.Array<StatModifier> Modifiers { get; set; } = new();

    /// <summary>HP paid to cast. Can't kill: the caster keeps at least 1 HP.</summary>
    [Export] public float HealthCost { get; set; }

    public override bool NeedsAim => false;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        if (HealthCost > 0f && ctx.Caster.GetParent() is Player p && p.Health.Current - HealthCost < p.Health.Max * 0.5f)
        {
            target = ctx.Origin;
            targetEnemy = null;
            return false;
        }

        return base.TryAutoTarget(ctx, out target, out targetEnemy);
    }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        Blast(ctx, ctx.Origin);

        if (ctx.Caster.GetParent() is not Player player)
            return;

        if (HealthCost > 0f)
            player.PayHealth(HealthCost);

        player.AddBuff(this, Modifiers, Duration, IconColor);
        FloatingText.Spawn(ctx.World, ctx.Origin + new Vector2(0, -player.Radius - 12f), DisplayName, IconColor, 16);
    }
}
