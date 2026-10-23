using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Second start-of-run screen: pick <see cref="ClassData.SpellPicks"/> spells from the class's
/// list. Pick order is slot order (first pick goes on key 1). Cards are built in code.
/// </summary>
public partial class SpellSelect : CanvasLayer
{
    [Signal] public delegate void SpellsChosenEventHandler(ClassData playerClass, Godot.Collections.Array<AbilityData> spells);
    [Signal] public delegate void BackRequestedEventHandler();

    private class Card
    {
        public AbilityData Spell;
        public StyleBoxFlat Style;
        public Button Button;
    }

    private Label _title;
    private Label _hint;
    private HBoxContainer _cards;
    private Button _back;
    private Button _start;

    private ClassData _class;
    private readonly List<Card> _cardList = new();
    private readonly List<AbilityData> _picks = new();

    /// <summary>Each spell's pick button; Autoplay presses them, then <see cref="StartButton"/>.</summary>
    public IEnumerable<(AbilityData Spell, Button Button)> Choices => _cardList.Select(c => (c.Spell, c.Button));
    public IReadOnlyList<AbilityData> Picks => _picks;
    public Button StartButton => _start;

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _hint = GetNode<Label>("%Hint");
        _cards = GetNode<HBoxContainer>("%Cards");
        _back = GetNode<Button>("%BackButton");
        _start = GetNode<Button>("%StartButton");

        _back.Pressed += () =>
        {
            Hide();
            EmitSignal(SignalName.BackRequested);
        };
        _start.Pressed += () =>
        {
            if (_picks.Count != _class.SpellPicks)
                return;
            Hide();
            EmitSignal(SignalName.SpellsChosen, _class, new Godot.Collections.Array<AbilityData>(_picks));
        };
        Hide();
    }

    public void Open(ClassData playerClass)
    {
        _class = playerClass;
        _picks.Clear();
        _cardList.Clear();
        foreach (var child in _cards.GetChildren())
            child.QueueFree();

        _title.Text = $"{playerClass.DisplayName}: choose {playerClass.SpellPicks} spells";
        foreach (var spell in playerClass.Spells)
            _cardList.Add(AddCard(spell));

        Refresh();
        Show();
        _cardList.FirstOrDefault()?.Button.GrabFocus();
    }

    private Card AddCard(AbilityData spell)
    {
        var card = new Card { Spell = spell, Style = UiStyles.Panel() };
        card.Style.BorderColor = UiStyles.Gold;

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(220, 300) };
        panel.AddThemeStyleboxOverride("panel", card.Style);
        _cards.AddChild(panel);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);

        content.AddChild(new ColorRect { Color = spell.IconColor, CustomMinimumSize = new Vector2(0, 40) });
        content.AddChild(UiStyles.WrappedLabel(spell.DisplayName, 20));

        var description = UiStyles.WrappedLabel(spell.Description, 14);
        description.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        content.AddChild(description);

        string stats = spell.Damage > 0f
            ? $"Damage {spell.Damage:0.#}  ·  Cooldown {spell.Cooldown:0.#}s"
            : $"Cooldown {spell.Cooldown:0.#}s";
        if (!spell.AutoCastByDefault)
            stats += "\nManual cast (moves you)";
        content.AddChild(UiStyles.WrappedLabel(stats, 13));

        card.Button = new Button { CustomMinimumSize = new Vector2(0, 40) };
        card.Button.Pressed += () => Toggle(spell);
        content.AddChild(card.Button);

        return card;
    }

    private void Toggle(AbilityData spell)
    {
        if (!_picks.Remove(spell))
        {
            if (_picks.Count >= _class.SpellPicks)
                return;
            _picks.Add(spell);
        }

        Refresh();
    }

    private void Refresh()
    {
        bool full = _picks.Count >= _class.SpellPicks;
        foreach (var card in _cardList)
        {
            int slot = _picks.IndexOf(card.Spell);
            card.Button.Text = slot >= 0 ? $"Key {slot + 1}  ·  Remove" : "Pick";
            card.Button.Disabled = slot < 0 && full;
            card.Style.SetBorderWidthAll(slot >= 0 ? 3 : 0);
        }

        _hint.Text = full
            ? "Picked in order: the first goes on key 1. Shift+key toggles auto-cast in play."
            : $"{_picks.Count} of {_class.SpellPicks} picked. Pick order sets the hotkeys.";
        _start.Text = $"Start run ({_picks.Count}/{_class.SpellPicks})";
        _start.Disabled = !full;
        if (full)
            _start.GrabFocus();
    }
}
