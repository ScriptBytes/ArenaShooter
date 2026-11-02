using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>Start-of-run screen listing the playable classes. Cards are built in code.</summary>
public partial class ClassSelect : CanvasLayer
{
    [Signal] public delegate void ClassChosenEventHandler(ClassData playerClass);
    [Signal] public delegate void BackRequestedEventHandler();

    private HBoxContainer _cards;
    private readonly List<(ClassData Class, Button Button)> _choices = new();

    /// <summary>Each class card's play button; Autoplay presses one to start a run.</summary>
    public IReadOnlyList<(ClassData Class, Button Button)> Choices => _choices;

    public override void _Ready()
    {
        _cards = GetNode<HBoxContainer>("%Cards");
        ScreenFit.Attach(_cards.GetParent<Control>());
        GetNode<Button>("%BackButton").Pressed += () =>
        {
            Hide();
            EmitSignal(SignalName.BackRequested);
        };
        Hide();
    }

    /// <param name="badge">Optional extra line per card (career mode: the character's level).</param>
    public void Open(IEnumerable<ClassData> classes, System.Func<ClassData, string> badge = null)
    {
        foreach (var child in _cards.GetChildren())
            child.QueueFree();

        _choices.Clear();
        Button first = null;
        foreach (var playerClass in classes)
        {
            var button = AddCard(playerClass, badge?.Invoke(playerClass));
            _choices.Add((playerClass, button));
            first ??= button;
        }

        Show();
        first?.GrabFocus();
    }

    private Button AddCard(ClassData playerClass, string badge)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(270, 0) };
        card.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        _cards.AddChild(card);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        card.AddChild(content);

        content.AddChild(new ColorRect { Color = playerClass.Color, CustomMinimumSize = new Vector2(0, 8) });
        if (playerClass.Sprite != null)
            content.AddChild(new TextureRect
            {
                Texture = playerClass.Sprite,
                CustomMinimumSize = new Vector2(0, 72),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            });
        content.AddChild(UiStyles.WrappedLabel(playerClass.DisplayName, 30));
        if (badge != null)
        {
            var label = UiStyles.WrappedLabel(badge, 16);
            label.AddThemeColorOverride("font_color", UiStyles.Gold);
            content.AddChild(label);
        }
        content.AddChild(UiStyles.WrappedLabel(playerClass.Description));
        content.AddChild(UiStyles.WrappedLabel(
            $"HP {playerClass.MaxHealth:0}  ·  Armor {playerClass.Armor:0}  ·  Speed {playerClass.MoveSpeed:0}", 14));
        content.AddChild(new HSeparator());

        content.AddChild(UiStyles.WrappedLabel($"Weapon: {playerClass.StartingWeapon.DisplayName}", 18));
        content.AddChild(UiStyles.WrappedLabel(playerClass.StartingWeapon.Weapon.Description, 14));
        if (playerClass.WeaponTraitText is { } trait)
        {
            var traitLabel = UiStyles.WrappedLabel(trait, 14);
            traitLabel.AddThemeColorOverride("font_color", UiStyles.Gold);
            content.AddChild(traitLabel);
        }
        content.AddChild(new HSeparator());

        content.AddChild(UiStyles.WrappedLabel($"Spells: pick {playerClass.SpellPicks} of {playerClass.Spells.Count}", 18));
        content.AddChild(UiStyles.WrappedLabel(string.Join(", ", playerClass.Spells.Select(s => s.DisplayName)), 14));

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
