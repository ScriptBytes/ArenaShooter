using Godot;

namespace ArenaShooter;

/// <summary>Hotbar slot for the health potion: a flask that empties once it's used this wave.</summary>
public partial class PotionSlotView : Control
{
    private const float SlotSize = 72f;
    private static readonly Color FlaskColor = new("e5484d");

    public Player Player { get; set; }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        if (Player != null)
        {
            TooltipText = $"Health Potion\nRestores {Player.PotionHeal:0} HP. {Player.PotionsPerWave} per wave.\n\n" +
                          "Press Space to drink";
        }
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        var rect = new Rect2(Vector2.Zero, Size);
        bool ready = Player != null && Player.PotionsLeft > 0;

        DrawRect(rect, new Color(0.08f, 0.08f, 0.1f, 0.85f));

        // Flask: round body with a neck. Full and red when ready, an empty outline once used.
        var center = new Vector2(Size.X / 2f, Size.Y / 2f + 6f);
        var glass = new Color(0.75f, 0.75f, 0.8f, ready ? 1f : 0.4f);
        if (ready)
            DrawCircle(center, 17f, FlaskColor);
        DrawArc(center, 17f, 0f, Mathf.Tau, 32, glass, 2f);
        var neck = new Rect2(center.X - 5f, center.Y - 29f, 10f, 12f);
        if (ready)
            DrawRect(neck, glass);
        else
            DrawRect(neck, glass, filled: false, width: 2f);

        if (Player != null && Player.PotionsPerWave > 1)
        {
            DrawOutlinedString(font, new Vector2(0, Size.Y - 4f), $"x{Player.PotionsLeft}",
                HorizontalAlignment.Right, Size.X - 4f, 12, Colors.White);
        }

        DrawRect(rect, ready ? FlaskColor : new Color(0.45f, 0.45f, 0.5f), filled: false, width: 2f);
        DrawOutlinedString(font, new Vector2(5f, 16f), "Space", HorizontalAlignment.Left, -1, 12, Colors.White);
    }

    private void DrawOutlinedString(Font font, Vector2 pos, string text, HorizontalAlignment align,
        float width, int size, Color color)
    {
        DrawStringOutline(font, pos, text, align, width, size, 4, Colors.Black);
        DrawString(font, pos, text, align, width, size, color);
    }
}
