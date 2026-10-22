using Godot;

namespace ArenaShooter;

/// <summary>
/// Base for anything that attacks on a cooldown: weapons (basic attacks) and spells alike.
/// Subclasses are Resources holding both the tuning data and the behavior (strategy pattern),
/// so a new ability is a .tres file, and a new *kind* of ability is a new subclass.
/// Resources are shared between users, so per-use state (cooldowns) lives in AbilitySlot.
/// </summary>
[GlobalClass]
public partial class AbilityData : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Color IconColor { get; set; } = Colors.White;
    [Export] public float Cooldown { get; set; } = 1f;
    [Export] public float Damage { get; set; } = 5f;
    [Export] public float Range { get; set; } = 300f;

    /// <summary>Starting auto-cast state for spell slots. Off for abilities that move the player.</summary>
    [Export] public bool AutoCastByDefault { get; set; } = true;

    [ExportGroup("On Hit")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float SlowMultiplier { get; set; } = 1f;
    [Export] public float SlowDuration { get; set; }
    [Export] public float Knockback { get; set; }

    /// <summary>Base crit chance in percent; the player's Crit Chance stat adds to it.</summary>
    [ExportGroup("Crit")]
    [Export(PropertyHint.Range, "0,100,1")] public float CritChance { get; set; } = 5f;

    /// <summary>Damage multiplier on a crit. Fast, light attacks get a low one; slow, heavy ones a high one.</summary>
    [Export] public float CritMultiplier { get; set; } = 1.5f;

    [ExportGroup("Feel")]
    /// <summary>Camera trauma (0–1) added when the ability lands. Keep it for big hits.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float ScreenShake { get; set; }

    public float EffectiveCritChance(AbilityContext ctx) =>
        Mathf.Clamp((CritChance + ctx.CritChanceBonus) / 100f, 0f, 1f);

    protected HitEffects Effects(AbilityContext ctx) =>
        new(SlowMultiplier, SlowDuration, Knockback, EffectiveCritChance(ctx), CritMultiplier);

    /// <summary>Manual casts aim at the mouse when true; otherwise they fire from the caster.</summary>
    public virtual bool NeedsAim => true;

    public float EffectiveRange(AbilityContext ctx) => Range + ctx.RangeBonus;

    /// <summary>
    /// Picks a target for auto-casting. Returns false when there's nothing worth casting at.
    /// <paramref name="targetEnemy"/> is the enemy aimed at, if any.
    /// </summary>
    public virtual bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        targetEnemy = Combat.NearestEnemy(ctx.Tree, ctx.Origin, EffectiveRange(ctx));
        target = targetEnemy?.GlobalPosition ?? ctx.Origin;
        return targetEnemy != null;
    }

    /// <summary>
    /// Performs the ability toward <paramref name="target"/> (global position).
    /// <paramref name="targetEnemy"/> is set for auto-casts aimed at an enemy, null for manual casts.
    /// </summary>
    public virtual void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
    }

    protected Vector2 ClampToRange(AbilityContext ctx, Vector2 target)
    {
        var offset = target - ctx.Origin;
        return ctx.Origin + offset.LimitLength(EffectiveRange(ctx));
    }
}
