using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Damage dealt per source (weapon, spell, minion type), biggest first: this wave's (since the last
/// <see cref="Combat.MarkWaveStart"/>, so in the shop it's the wave that just ended) or the whole
/// run's. Reads <see cref="Combat.DamageBySource"/> on its own a few times a second, so the HUD and
/// the shop only have to add it. With <see cref="ShowTabs"/> the player switches between the two;
/// without, it shows the wave and hides itself until something has dealt damage.
/// </summary>
public partial class DamageMeter : VBoxContainer
{
    /// <summary>Adds "Last wave" / "Whole run" buttons (the shop). Set before adding it to the tree.</summary>
    public bool ShowTabs { get; init; }

    private bool _wholeRun;
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
        if (ShowTabs)
            AddTabs();
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

    private void AddTabs()
    {
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 6);
        var group = new ButtonGroup();
        foreach (var (label, wholeRun) in new[] { ("Last wave", false), ("Whole run", true) })
        {
            var tab = new Button
            {
                Text = label,
                ToggleMode = true,
                ButtonGroup = group,
                ButtonPressed = wholeRun == _wholeRun,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                FocusMode = FocusModeEnum.None,
            };
            tab.AddThemeFontSizeOverride("font_size", 13);
            tab.Toggled += on =>
            {
                if (!on)
                    return;
                _wholeRun = wholeRun;
                Refresh();
            };
            tabs.AddChild(tab);
        }
        AddChild(tabs);
    }

    public void Refresh()
    {
        var sources = Combat.DamageBySource
            .Select(p => (Source: p.Key, Damage: _wholeRun ? p.Value : p.Value - Combat.DamageAtWaveStart.GetValueOrDefault(p.Key)))
            .Where(p => p.Damage >= 0.5f)
            .OrderByDescending(p => p.Damage)
            .ToList();
        // The HUD's meter only appears once the wave's first hit lands; the shop's keeps its tabs.
        Visible = ShowTabs || sources.Count > 0;
        string scope = _wholeRun ? "this run" : ShowTabs ? "last wave" : "this wave";
        _title.Text = $"Damage {scope}: {Format(sources.Sum(p => p.Damage))}";
        sources = sources.Take(MaxRows).ToList();

        while (_rows.Count < sources.Count)
        {
            var row = new Row { CustomMinimumSize = new Vector2(0, RowHeight) };
            _rows.Add(row);
            AddChild(row);
        }
        float top = sources.Count > 0 ? sources[0].Damage : 1f;
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            row.Visible = i < sources.Count;
            if (!row.Visible)
                continue;
            var (source, damage) = sources[i];
            row.Source = source;
            row.Value = Format(damage);
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

    // One bar: a fill in the source's color, the name on the left, the damage on the right.
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
