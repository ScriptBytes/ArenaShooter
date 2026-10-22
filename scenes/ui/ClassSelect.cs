using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>Start-of-run screen listing the playable classes. Cards are built in code.</summary>
public partial class ClassSelect : CanvasLayer
{
    [Signal] public delegate void ClassChosenEventHandler(ClassData playerClass);

    private HBoxContainer _cards;
    private readonly List<(ClassData Class, Button Button)> _choices = new();

    /// <summary>Each class card's play button; Autoplay presses one to start a run.</summary>
    public IReadOnlyList<(ClassData Class, Button Button)> Choices => _choices;

    public override void _Ready()
    {
        _cards = GetNode<HBoxContainer>("%Cards");
        Hide();
    }

    public void Open(IEnumerable<ClassData> classes)
    {
        foreach (var child in _cards.GetChildren())
            child.QueueFree();

        _choices.Clear();
        Button first = null;
        foreach (var playerClass in classes)
        {
            var button = AddCard(playerClass);
            _choices.Add((playerClass, button));
            first ??= button;
        }

        Show();
        first?.GrabFocus();
    }

    private Button AddCard(ClassData playerClass)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(320, 0) };
        card.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        _cards.AddChild(card);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        card.AddChild(content);

        content.AddChild(new ColorRect { Color = playerClass.Color, CustomMinimumSize = new Vector2(0, 8) });
        content.AddChild(UiStyles.WrappedLabel(playerClass.DisplayName, 30));
        content.AddChild(UiStyles.WrappedLabel(playerClass.Description));
        content.AddChild(UiStyles.WrappedLabel(
            $"HP {playerClass.MaxHealth:0}  ·  Armor {playerClass.Armor:0}  ·  Speed {playerClass.MoveSpeed:0}", 14));
        content.AddChild(new HSeparator());

        content.AddChild(UiStyles.WrappedLabel($"Weapon: {playerClass.Weapon.DisplayName}", 18));
        content.AddChild(UiStyles.WrappedLabel(playerClass.Weapon.Description, 14));
        content.AddChild(new HSeparator());

        var spells = playerClass.Spells.Select((s, i) => $"{i + 1}. {s.DisplayName} — {s.Description}");
        content.AddChild(UiStyles.WrappedLabel("Spells", 18));
        content.AddChild(UiStyles.WrappedLabel(string.Join("\n", spells), 14));

        var button = new Button
        {
            Text = $"Play {playerClass.DisplayName}",
            CustomMinimumSize = new Vector2(0, 44),
            SizeFlagsVertical = Control.SizeFlags.ShrinkEnd | Control.SizeFlags.Expand,
        };
        button.Pressed += () =>
        {
            Hide();
            EmitSignal(SignalName.ClassChosen, playerClass);
        };
        content.AddChild(button);

        return button;
    }
}
