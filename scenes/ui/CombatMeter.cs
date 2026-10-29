using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// One <see cref="MeterLog"/> (damage or healing) as bars per source, biggest first: this wave's
/// (since the last <see cref="Combat.MarkWaveStart"/>, so in the shop it's the wave that just ended)
/// or the whole run's. Reads the log on its own a few times a second, so screens only have to add it
/// (usually through <see cref="CombatMeters"/>).
/// </summary>
public partial class CombatMeter : VBoxContainer
{
    private const float RefreshInterval = 0.25f;
    private const int FontSize = 13;
    private const float RowHeight = 19f;
    private static readonly Color FallbackColor = new("8a8f98");

    /// <summary>"Damage" or "Healing": the title's first word.</summary>
    public string Title { get; set; } = "";

    public MeterLog Log { get; set; }

    public bool WholeRun { get; set; }

    /// <summary>The wave view reads "last wave" (the shop) instead of "this wave" (the HUD).</summary>
    public bool AfterWave { get; init; }

    /// <summary>Hides the whole meter while the view is empty (the HUD); otherwise it says so.</summary>
    public bool HideWhenEmpty { get; init; }

    public int MaxRows { get; init; } = 8;

    private Label _title;
    private Label _empty;
    private readonly List<Row> _rows = new();
    private float _refreshLeft;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 3);
        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", 15);
        AddChild(_title);
        _empty = new Label { Text = "Nothing yet" };
        _empty.AddThemeFontSizeOverride("font_size", FontSize);
        _empty.AddThemeColorOverride("font_color", UiStyles.Dim);
        AddChild(_empty);
        Refresh();
    }

    public override void _Process(double delta)
    {
        // Keeps checking while hidden for lack of data, but not while the screen around it is closed.
        _refreshLeft -= (float)delta;
        if (_refreshLeft > 0f || GetParent() is CanvasItem parent && !parent.IsVisibleInTree())
            return;
        _refreshLeft = RefreshInterval;
        Refresh();
    }

    public void Refresh()
    {
        if (_title == null || Log == null)
            return;

        var sources = Log.Totals.Keys
            .Select(source => (Source: source, Amount: WholeRun ? Log.Totals[source] : Log.ThisWave(source)))
            .Where(p => p.Amount >= 0.5f)
            .OrderByDescending(p => p.Amount)
            .ToList();
        if (HideWhenEmpty)
            Visible = sources.Count > 0;
        _empty.Visible = sources.Count == 0;
        string scope = WholeRun ? "this run" : AfterWave ? "last wave" : "this wave";
        _title.Text = $"{Title} {scope}: {Format(sources.Sum(p => p.Amount))}";
        sources = sources.Take(MaxRows).ToList();

        while (_rows.Count < sources.Count)
        {
            var row = new Row { CustomMinimumSize = new Vector2(0, RowHeight) };
            _rows.Add(row);
            AddChild(row);
        }
        float top = sources.Count > 0 ? sources[0].Amount : 1f;
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            row.Visible = i < sources.Count;
            if (!row.Visible)
                continue;
            var (source, amount) = sources[i];
            row.Source = source;
            row.Value = Format(amount);
            row.Fill = amount / top;
            row.Color = Log.ColorOf(source, FallbackColor);
            row.QueueRedraw();
        }
    }

    private static string Format(float amount) => amount switch
    {
        >= 1_000_000f => $"{amount / 1_000_000f:0.0}M",
        >= 10_000f => $"{amount / 1000f:0.0}k",
        _ => $"{amount:0}",
    };

    // One bar: a fill in the source's color, the name on the left, the amount on the right.
    private partial class Row : Control
    {
        public string Source = "";
        public string Value = "";
        public float Fill;
        public Color Color;

        public Row() => MouseFilter = MouseFilterEnum.Ignore;

        public override void _Draw()
        {
            var size = Size;
            DrawRect(new Rect2(Vector2.Zero, size), new Color(0f, 0f, 0f, 0.45f));
            DrawRect(new Rect2(Vector2.Zero, new Vector2(size.X * Fill, size.Y)), new Color(Color.Darkened(0.35f), 0.7f));
            DrawRect(new Rect2(Vector2.Zero, new Vector2(3f, size.Y)), Color);

            var font = GetThemeDefaultFont();
            float baseline = (size.Y + font.GetAscent(FontSize) - font.GetDescent(FontSize)) / 2f;
            float valueWidth = font.GetStringSize(Value, HorizontalAlignment.Left, -1, FontSize).X;
            float right = size.X - 6f;

            DrawString(font, new Vector2(right - valueWidth, baseline), Value, HorizontalAlignment.Left, -1, FontSize, Colors.White);
            float nameWidth = right - valueWidth - 14f;
            DrawString(font, new Vector2(8f, baseline), Source, HorizontalAlignment.Left, nameWidth, FontSize, Colors.White);
        }
    }
}
