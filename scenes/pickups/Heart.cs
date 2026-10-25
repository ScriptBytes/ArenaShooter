using Godot;

namespace ArenaShooter;

/// <summary>
/// Rare enemy drop. Heals the player directly when they're hurt; at full HP it fills the potion
/// reserve instead (see PotionBelt, Player.CollectHeart). Waits on the floor while the player is at
/// full HP and the reserve is full. A heart worth more than the room left tops up and the rest is lost.
/// Cleared at wave end like coins.
/// </summary>
public partial class Heart : Pickup
{
    public static readonly Color HeartColor = new("ff5f7e");

    public float Value { get; set; } = 2f;

    protected override bool Wanted => Player.WantsHeart;

    protected override void Collect() => Player.CollectHeart(Value);

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
