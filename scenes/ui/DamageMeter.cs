using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Damage dealt this run per source (weapon, spell, minion type), biggest first, with this wave's
/// share in brackets. Reads <see cref="Combat.DamageBySource"/> on its own a few times a second,
/// so the HUD and the shop only have to add it. Hidden until something has dealt damage.
/// </summary>
public partial class DamageMeter : VBoxContainer
{
    private const float RefreshInterval = 0.25f;
    private const int MaxRows = 8;
    private const int FontSize = 13;
    private const float RowHeight = 19f;
    private static readonly Color FallbackColor = new("8a8f98");

    private Label _title;
    private readonly List<Row> _rows = new();
    private float _refreshLeft;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 3);
        _title = new Label();
        _title.AddThemeFontSizeOverride("font_size", 15);
        AddChild(_title);
        Refresh();
    }

    public override void _Process(double delta)
    {
        // Keeps checking while hidden for lack of damage, but not while the screen around it is closed.
        _refreshLeft -= (float)delta;
        if (_refreshLeft > 0f || GetParent() is CanvasItem parent && !parent.IsVisibleInTree())
            return;
        _refreshLeft = RefreshInterval;
        Refresh();
    }

    public void Refresh()
    {
        var sources = Combat.DamageBySource.OrderByDescending(p => p.Value).Take(MaxRows).ToList();
        Visible = sources.Count > 0;
        if (!Visible)
            return;

        float total = Combat.DamageBySource.Values.Sum();
        float waveTotal = total - Combat.DamageAtWaveStart.Values.Sum();
        _title.Text = $"Damage {Format(total)}  (+{Format(waveTotal)} this wave)";

        while (_rows.Count < sources.Count)
        {
            var row = new Row { CustomMinimumSize = new Vector2(0, RowHeight) };
            _rows.Add(row);
            AddChild(row);
        }
        float top = sources[0].Value;
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            row.Visible = i < sources.Count;
            if (!row.Visible)
                continue;
            var (source, damage) = sources[i];
            row.Source = source;
            row.Value = Format(damage);
            float wave = damage - Combat.DamageAtWaveStart.GetValueOrDefault(source);
            row.Wave = wave >= 0.5f ? $"+{Format(wave)}" : "";
            row.Fill = damage / top;
            row.Color = Combat.SourceColors.GetValueOrDefault(source, FallbackColor);
            row.QueueRedraw();
        }
    }

    private static string Format(float damage) => damage switch
    {
        >= 1_000_000f => $"{damage / 1_000_000f:0.0}M",
        >= 10_000f => $"{damage / 1000f:0.0}k",
        _ => $"{damage:0}",
    };

    // One bar: a fill in the source's color, the name on the left, total and this wave on the right.
    private partial class Row : Control
    {
        public string Source = "";
        public string Value = "";
        public string Wave = "";
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
            float waveWidth = Wave == "" ? 0f : font.GetStringSize(Wave, HorizontalAlignment.Left, -1, FontSize).X + 6f;
            float right = size.X - 6f;

            DrawString(font, new Vector2(right - valueWidth, baseline), Value, HorizontalAlignment.Left, -1, FontSize, Colors.White);
            if (Wave != "")
                DrawString(font, new Vector2(right - valueWidth - waveWidth, baseline), Wave,
                    HorizontalAlignment.Left, -1, FontSize, UiStyles.Dim);
            float nameWidth = right - valueWidth - waveWidth - 14f;
            DrawString(font, new Vector2(8f, baseline), Source, HorizontalAlignment.Left, nameWidth, FontSize, Colors.White);
        }
    }
}
