using Godot;

namespace ArenaShooter;

/// <summary>
/// Hotbar slot for a potion, drawn as a flask whose liquid level shows what's left. Three kinds:
/// the health potion (Space: free charges plus bought ones), the reserve that hearts fill (drunk
/// by Space when no full potion is left), and a bought buff potion (keys 5+).
/// </summary>
public partial class PotionSlotView : Control
{
    public enum SlotKind { Health, Reserve, Buff }

    private const float SlotSize = 72f;
    public static readonly Color HealthColor = new("e5484d");

    public Player Player { get; set; }
    public SlotKind Kind { get; set; }

    /// <summary>Buff slots only: the potion and its hotkey number (5, 6…).</summary>
    public ConsumableData Consumable { get; set; }
    public int Hotkey { get; set; }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        if (Player == null)
            return;

        TooltipText = Kind switch
        {
            SlotKind.Health => $"Health Potion\nRestores {Player.PotionHeal:0} HP. {Player.PotionsPerWave} free per wave, " +
                               "plus any bought in the shop.\n\nPress Space to drink",
            SlotKind.Reserve => $"Reserve Potion\nHearts fill it, up to {Player.PotionHeal:0} HP. It keeps between waves.\n" +
                                "While it's full, hearts stay on the floor.\n\nSpace drinks it when no full potion is left",
            _ => $"{Consumable.DisplayName}\n{Consumable.Describe()}\nBuy more in the shop.\n\nPress {Hotkey} to drink",
        };
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Player == null)
            return;

        var potions = Player.Potions;
        (Color color, float fill, int count, string key) = Kind switch
        {
            SlotKind.Health => (HealthColor, potions.HealthPotions > 0 ? 1f : 0f, potions.HealthPotions, "Space"),
            SlotKind.Reserve => (HealthColor.Lerp(Heart.HeartColor, 0.5f),
                potions.ReserveCapacity > 0f ? Mathf.Clamp(potions.Reserve / potions.ReserveCapacity, 0f, 1f) : 0f,
                -1, null),
            _ => (Consumable.IconColor, potions.Count(Consumable) > 0 ? 1f : 0f, potions.Count(Consumable), Hotkey.ToString()),
        };

        var font = ThemeDB.FallbackFont;
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, new Color(0.08f, 0.08f, 0.1f, 0.85f));

        // Flask: a round body with a neck. The liquid fills the body from the bottom.
        const float r = 17f;
        var center = new Vector2(Size.X / 2f, Size.Y / 2f + 6f);
        bool any = fill > 0f;
        var glass = new Color(0.75f, 0.75f, 0.8f, any ? 1f : 0.4f);
        if (fill >= 1f)
            DrawCircle(center, r, color);
        else if (any)
            DrawLiquid(center, r, fill, color);
        DrawArc(center, r, 0f, Mathf.Tau, 32, glass, 2f);
        var neck = new Rect2(center.X - 5f, center.Y - r - 12f, 10f, 12f);
        if (fill >= 1f)
            DrawRect(neck, glass);
        else
            DrawRect(neck, glass, filled: false, width: 2f);

        if (Kind == SlotKind.Reserve)
        {
            // No hotkey of its own (Space drinks it), so the corners hold its name and fill instead.
            DrawOutlinedString(font, new Vector2(0, 16f), $"{potions.Reserve:0.#}/{potions.ReserveCapacity:0}",
                HorizontalAlignment.Right, Size.X - 4f, 11, Colors.White);
            DrawOutlinedString(font, new Vector2(5f, Size.Y - 4f), "Reserve", HorizontalAlignment.Left, -1, 11, UiStyles.Dim);
        }
        else if (count != 1)
        {
            DrawOutlinedString(font, new Vector2(0, Size.Y - 4f), $"x{count}",
                HorizontalAlignment.Right, Size.X - 4f, 12, count > 0 ? Colors.White : UiStyles.Dim);
        }

        var border = any ? color : new Color(0.45f, 0.45f, 0.5f);
        DrawRect(rect, border, filled: false, width: 2f);
        if (key != null)
            DrawOutlinedString(font, new Vector2(5f, 16f), key, HorizontalAlignment.Left, -1, 12, Colors.White);
    }

    // The part of the circle below the liquid line, as a polygon (a circular segment).
    private void DrawLiquid(Vector2 center, float r, float fill, Color color)
    {
        float surface = center.Y + r - 2f * r * fill;
        float half = Mathf.Acos(Mathf.Clamp((surface - center.Y) / r, -1f, 1f)); // angle from straight down
        const int steps = 24;
        var points = new Vector2[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float a = Mathf.Pi / 2f - half + 2f * half * i / steps; // sweeps through the bottom
            points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        if (points.Length >= 3)
            DrawColoredPolygon(points, color);
    }

    private void DrawOutlinedString(Font font, Vector2 pos, string text, HorizontalAlignment align,
        float width, int size, Color color)
    {
        DrawStringOutline(font, pos, text, align, width, size, 4, Colors.Black);
        DrawString(font, pos, text, align, width, size, color);
    }
}
