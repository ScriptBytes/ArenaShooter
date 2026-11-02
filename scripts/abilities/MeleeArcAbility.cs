using Godot;

namespace ArenaShooter;

/// <summary>Swing that hits every enemy in a wedge in front of the caster.</summary>
[GlobalClass]
public partial class MeleeArcAbility : AbilityData
{
    // Range items are tuned for ranged attacks; melee reach gets half the bonus.
    private const float RangeBonusScale = 0.5f;

    [ExportGroup("Arc")]
    [Export(PropertyHint.Range, "10,360,5")] public float ArcDegrees { get; set; } = 120f;

    public override bool IsMelee => true;

    private float Reach(AbilityContext ctx) => Range + ctx.RangeBonus * RangeBonusScale;

    public override bool TryAutoTarget(AbilityContext ctx, out Vector2 target, out Enemy targetEnemy)
    {
        targetEnemy = Combat.NearestEnemy(ctx.Tree, ctx.Origin, Reach(ctx));
        target = targetEnemy?.GlobalPosition ?? ctx.Origin;
        return targetEnemy != null;
    }

    public override void Cast(AbilityContext ctx, Vector2 target, Enemy targetEnemy)
    {
        float reach = Reach(ctx);
        float halfArc = Mathf.DegToRad(ArcDegrees) / 2f;
        var facing = ctx.Origin.DirectionTo(target);
        if (facing == Vector2.Zero)
            facing = Vector2.Right;

        foreach (var enemy in Combat.EnemiesInRadius(ctx.Tree, ctx.Origin, reach))
        {
            var toEnemy = enemy.GlobalPosition - ctx.Origin;
            // Enemies overlapping the caster always get hit, whatever the angle.
            if (toEnemy.Length() < 20f || Mathf.Abs(facing.AngleTo(toEnemy)) <= halfArc)
                Combat.Hit(enemy, Damage * ctx.DamageMultiplier, Effects(ctx), ctx.Origin);
        }

        AreaFlash.Spawn(ctx.World, ctx.Origin, reach, IconColor, halfArc * 2f, facing.Angle());
    }
}
