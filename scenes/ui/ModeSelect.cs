using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>First screen: Arcade (one fresh run) or Career (characters that grow between runs). Built in code.</summary>
public partial class ModeSelect : CanvasLayer
{
    [Signal] public delegate void ModeChosenEventHandler(int mode);

    private readonly List<(GameMode Mode, Button Button)> _choices = new();

    /// <summary>Each mode's button; Autoplay presses one (--mode).</summary>
    public IReadOnlyList<(GameMode Mode, Button Button)> Choices => _choices;

    public override void _Ready()
    {
        var layout = UiStyles.ScreenLayout(this, 24);
        layout.AddChild(UiStyles.Title("Choose a mode", 40));

        var cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cards.AddThemeConstantOverride("separation", 16);
        layout.AddChild(cards);

        AddCard(cards, GameMode.Arcade, "Arcade",
            "One run of 5 waves with any class. Everything starts fresh and is gone when the run ends.",
            "Nothing is saved.");
        AddCard(cards, GameMode.Career, "Career",
            "Each class is a character that levels up from every run, win or lose. Spend talent points on " +
            "permanent bonuses, and after a victory keep one piece of gear as an heirloom for the next run.",
            "Progress saves automatically.");
        Hide();
    }

    public void Open()
    {
        Show();
        _choices[0].Button.GrabFocus();
    }

    private void AddCard(HBoxContainer cards, GameMode mode, string title, string description, string note)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(340, 260) };
        card.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        cards.AddChild(card);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        card.AddChild(content);

        content.AddChild(new ColorRect
        {
            Color = mode == GameMode.Career ? UiStyles.Gold : UiStyles.Dim,
            CustomMinimumSize = new Vector2(0, 8),
        });
        content.AddChild(UiStyles.WrappedLabel(title, 30));
        content.AddChild(UiStyles.WrappedLabel(description));
        content.AddChild(UiStyles.DimLabel(note));

        var button = new Button
        {
            Text = $"Play {title}",
            CustomMinimumSize = new Vector2(0, 44),
            SizeFlagsVertical = Control.SizeFlags.ShrinkEnd | Control.SizeFlags.Expand,
        };
        button.Pressed += () =>
        {
            Hide();
            EmitSignal(SignalName.ModeChosen, (int)mode);
        };
        content.AddChild(button);
        _choices.Add((mode, button));
    }
}
