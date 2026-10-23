using Godot;

namespace ArenaShooter;

/// <summary>
/// Rare enemy drop that fills the potion reserve (see PotionBelt) instead of healing directly.
/// Ignores the player while the reserve is full, so it waits on the floor until there's room.
/// A heart worth more than the room left tops the reserve up and the rest is lost.
/// Cleared at wave end like coins.
/// </summary>
public partial class Heart : Pickup
{
    public static readonly Color HeartColor = new("ff5f7e");

    public float Value { get; set; } = 2f;

    protected override bool Wanted => !Player.Potions.ReserveFull;

    protected override void Collect() => Player.FillReserve(Value);

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
