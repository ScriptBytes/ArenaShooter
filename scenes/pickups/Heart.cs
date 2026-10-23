using Godot;

namespace ArenaShooter;

/// <summary>
/// Rare enemy drop that restores a little health. Ignores the player while they're at full
/// health, so it waits on the floor until it's useful. Cleared at wave end like coins.
/// </summary>
public partial class Heart : Pickup
{
    public static readonly Color HeartColor = new("ff5f7e");

    public float HealAmount { get; set; } = 2f;

    protected override bool Wanted => Player.Health.Current < Player.Health.Max;

    protected override void Collect() => Player.Heal(HealAmount);

    public override void _Draw()
    {
        // Two lobes over a point. A soft pulse makes it stand out from the coins.
        float pulse = 1f + 0.12f * Mathf.Sin(Time.GetTicksMsec() / 160f);
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One * pulse);
        DrawCircle(new Vector2(-3.5f, -2f), 4.5f, HeartColor);
        DrawCircle(new Vector2(3.5f, -2f), 4.5f, HeartColor);
        DrawColoredPolygon([new Vector2(-7.8f, -0.5f), new Vector2(7.8f, -0.5f), new Vector2(0, 8f)], HeartColor);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        QueueRedraw();
    }
}
