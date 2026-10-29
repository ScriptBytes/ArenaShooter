using Godot;

namespace ArenaShooter;

/// <summary>
/// Gold dropped by enemies. Coins left when the wave ends go into the bag (see RunState.Bag).
/// </summary>
public partial class Coin : Pickup
{
    public static readonly Color GoldColor = new("f5c542");

    private const float SpriteSize = 18f;
    private static readonly Texture2D Sprite = GD.Load<Texture2D>("res://art/pickups/coin.svg");

    public int Value { get; set; } = 1;

    protected override void Collect() => Player.CollectGold(Value);

    public override void _Ready()
    {
        base._Ready();
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
    }

    public override void _Draw()
    {
        if (Sprite != null)
        {
            DrawTextureRect(Sprite, new Rect2(-Vector2.One * SpriteSize / 2f, Vector2.One * SpriteSize), false);
            return;
        }
        const float s = 6f;
        DrawColoredPolygon(
            [new Vector2(0, -s), new Vector2(s, 0), new Vector2(0, s), new Vector2(-s, 0)],
            GoldColor);
    }
}
