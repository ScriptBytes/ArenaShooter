using Godot;

namespace ArenaShooter;

/// <summary>
/// Drawing helpers for the placeholder sprites (art/). The player, enemies and minions draw
/// their sprite from their own _Draw(), so the existing effects (hit pop, leap arc, afterimages)
/// keep working on top. Sprites are authored facing right and mirrored when moving left.
/// </summary>
public static class SpriteArt
{
    /// <summary>
    /// Modulate for the white hit flash. Modulate multiplies the texture, so plain white would
    /// change nothing; values above 1 wash the sprite out toward white instead.
    /// </summary>
    public static readonly Color Flash = new(4f, 4f, 4f);

    private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.3f);

    /// <summary>Draws <paramref name="texture"/> into a <paramref name="size"/>-pixel square centered on <paramref name="center"/>.</summary>
    public static void Draw(CanvasItem item, Texture2D texture, Vector2 center, float size, bool faceLeft, Color modulate)
    {
        var rect = new Rect2(center - Vector2.One * size / 2f, Vector2.One * size);
        // A negative width mirrors the texture in place (same rect, not measured from the right edge).
        if (faceLeft)
            rect = rect with { Size = new Vector2(-size, size) };
        item.DrawTextureRect(texture, rect, false, modulate);
    }

    /// <summary>A flat ellipse under the feet, so sprites read as standing on the floor.</summary>
    public static void DrawShadow(CanvasItem item, Vector2 center, float width, float alpha = 1f)
    {
        var points = new Vector2[16];
        for (int i = 0; i < points.Length; i++)
            points[i] = center + Vector2.FromAngle(Mathf.Tau * i / points.Length) * new Vector2(width / 2f, width / 6f);
        item.DrawColoredPolygon(points, new Color(ShadowColor, ShadowColor.A * alpha));
    }

    /// <summary>Where the shadow goes for a sprite of <paramref name="size"/>: near the bottom of its square.</summary>
    public static Vector2 Feet(float size) => new(0f, size * 0.42f);
}
