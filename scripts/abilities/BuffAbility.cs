using Godot;

namespace ArenaShooter;

/// <summary>
/// Gives the caster timed stat bonuses (War Cry, Shield Wall), plus an optional blast around
/// them: set Damage to 0 for a pure shove. Auto-casts once <see cref="AreaAbility.MinTargets"/>
/// enemies are within the radius, so the buff isn't wasted on an empty arena.
/// </summary>
[GlobalClass]
public partial class BuffAbility : AreaAbility
{
    [ExportGroup("Buff")]
    [Export] public float Duration { get; set; } = 5f;
    [Export] public Godot.Collections.Array<StatModifier> Modifiers { get; set; } = new();

    public override bool NeedsAim => false;

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        Blast(ctx, ctx.Origin);

        if (ctx.Caster.GetParent() is not Player player)
            return;

        player.AddBuff(this, Modifiers, Duration, IconColor);
        FloatingText.Spawn(ctx.World, ctx.Origin + new Vector2(0, -player.Radius - 12f), DisplayName, IconColor, 16);
    }
}
