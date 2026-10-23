using Godot;

namespace ArenaShooter;

/// <summary>Shared styling for code-built UI. Candidate to become a Godot Theme resource later.</summary>
public static class UiStyles
{
    public static readonly Color Gold = new(0.96f, 0.77f, 0.26f);
    public static readonly Color Dim = new(0.65f, 0.67f, 0.72f);

    public static StyleBoxFlat Panel()
    {
        var style = new StyleBoxFlat { BgColor = new Color(0.12f, 0.12f, 0.15f) };
        style.SetCornerRadiusAll(6);
        style.SetContentMarginAll(12);
        return style;
    }

    /// <summary>Hover tooltips styled like the panels. Set it as the Theme of a container to cover its children.</summary>
    public static Theme TooltipTheme()
    {
        var style = Panel();
        style.BgColor = new Color(0.18f, 0.18f, 0.22f);
        style.BorderColor = Dim;
        style.SetBorderWidthAll(1);
        style.SetContentMarginAll(8);

        var theme = new Theme();
        theme.SetStylebox("panel", "TooltipPanel", style);
        theme.SetFontSize("font_size", "TooltipLabel", 15);
        return theme;
    }

    /// <summary>
    /// Fills a full-screen menu layer with the standard dark backdrop and a centered column, and
    /// returns the column. For screens built entirely in code (mode select, career screens).
    /// </summary>
    public static VBoxContainer ScreenLayout(CanvasLayer layer, int separation = 20)
    {
        layer.AddChild(new ColorRect
        {
            Color = new Color(0.03f, 0.03f, 0.05f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
        });
        var center = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        layer.AddChild(center);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", separation);
        center.AddChild(layout);
        return layout;
    }

    /// <summary>A gold-filled progress bar for XP.</summary>
    public static ProgressBar XpBar()
    {
        var bar = new ProgressBar { CustomMinimumSize = new Vector2(0, 18), ShowPercentage = false };
        var fill = new StyleBoxFlat { BgColor = Gold };
        fill.SetCornerRadiusAll(4);
        var background = new StyleBoxFlat { BgColor = new Color(0.16f, 0.16f, 0.2f) };
        background.SetCornerRadiusAll(4);
        bar.AddThemeStyleboxOverride("fill", fill);
        bar.AddThemeStyleboxOverride("background", background);
        return bar;
    }

    public static Label Title(string text, int fontSize = 36)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    public static Label DimLabel(string text, int fontSize = 14)
    {
        var label = WrappedLabel(text, fontSize);
        label.AddThemeColorOverride("font_color", Dim);
        return label;
    }

    public static Label WrappedLabel(string text, int fontSize = 16)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }
}
