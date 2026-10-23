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

    public static Label WrappedLabel(string text, int fontSize = 16)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }
}
