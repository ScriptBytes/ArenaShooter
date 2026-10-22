using Godot;

namespace ArenaShooter;

/// <summary>
/// Floating number that pops up where damage landed, drifts upward and fades.
/// Bigger hits draw bigger numbers. Purely visual.
/// </summary>
public partial class DamageNumber : Node2D
{
    private const float Duration = 0.65f;
    private const float PopTime = 0.1f;
    private static readonly Color OutlineColor = new(0f, 0f, 0f, 0.85f);
    private static readonly Color CritColor = new("ffd84a");

    public string Text { get; set; } = "";
    public Color Color { get; set; } = Colors.White;
    public int FontSize { get; set; } = 14;

    private Vector2 _velocity;
    private float _time;

    /// <summary>
    /// Adds a number to the world. Deferred so it's safe from physics callbacks.
    /// Crits draw in yellow, larger, with a "!".
    /// </summary>
    public static void Spawn(Node world, Vector2 position, float amount, Color color, bool crit = false)
    {
        string text = amount < 1f ? amount.ToString("0.#") : Mathf.RoundToInt(amount).ToString();
        float fontSize = Mathf.Clamp(12f + amount * 0.4f, 14f, 28f);
        var number = new DamageNumber
        {
            Position = position + new Vector2((float)GD.RandRange(-6.0, 6.0), -10f),
            Text = crit ? text + "!" : text,
            Color = crit ? CritColor : color,
            FontSize = (int)(crit ? fontSize * 1.3f : fontSize),
        };
        world.CallDeferred(Node.MethodName.AddChild, number);
    }

    public override void _Ready()
    {
        AddToGroup(Groups.Transient);
        ZIndex = 10;
        _velocity = new Vector2((float)GD.RandRange(-30.0, 30.0), -110f);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        Position += _velocity * dt;
        _velocity.Y += 220f * dt; // slows the rise into a small arc
        QueueRedraw();

        if (_time >= Duration)
            QueueFree();
    }

    public override void _Draw()
    {
        // Starts oversized and snaps down to normal size, then fades over the last 40%.
        float scale = 1f + 0.5f * Mathf.Max(1f - _time / PopTime, 0f);
        float alpha = Mathf.Clamp((Duration - _time) / (Duration * 0.4f), 0f, 1f);

        var font = ThemeDB.FallbackFont;
        var size = font.GetStringSize(Text, HorizontalAlignment.Left, -1, FontSize);
        var origin = new Vector2(-size.X / 2f, size.Y / 4f);

        DrawSetTransform(Vector2.Zero, 0f, Vector2.One * scale);
        DrawStringOutline(font, origin, Text, HorizontalAlignment.Left, -1, FontSize, 4,
            new Color(OutlineColor, OutlineColor.A * alpha));
        DrawString(font, origin, Text, HorizontalAlignment.Left, -1, FontSize, new Color(Color, alpha));
    }
}
