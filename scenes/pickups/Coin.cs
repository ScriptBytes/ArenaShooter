using Godot;

namespace ArenaShooter;

/// <summary>
/// Gold dropped by enemies. Coins left when the wave ends go into the bag (see RunState.Bag).
/// </summary>
public partial class Coin : Pickup
{
    public static readonly Color GoldColor = new("f5c542");

    public int Value { get; set; } = 1;

    protected override void Collect() => Player.CollectGold(Value);

    public override void _Draw()
    {
        const float s = 6f;
        DrawColoredPolygon(
            [new Vector2(0, -s), new Vector2(s, 0), new Vector2(0, s), new Vector2(-s, 0)],
            GoldColor);
    }
}
