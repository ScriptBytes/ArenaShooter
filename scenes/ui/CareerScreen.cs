using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Career mode's between-run screen, shown after class select: the character's level and record,
/// its heirloom, and the talents to spend points on (saved on every click; reset is free).
/// "Reset character" erases the whole character, after an "are you sure" prompt. Built in code.
/// </summary>
public partial class CareerScreen : CanvasLayer
{
    [Signal] public delegate void ContinueRequestedEventHandler(ClassData playerClass);
    [Signal] public delegate void BackRequestedEventHandler();

    private Label _title;
    private ProgressBar _xpBar;
    private Label _xpLabel;
    private Label _record;
    private Label _heirloom;
    private Label _points;
    private GridContainer _talentGrid;
    private Button _reset;
    private Button _continue;
    private VBoxContainer _content;
    private VBoxContainer _confirm;
    private Label _confirmText;
    private Button _resetCharacter;
    private Button _confirmYes;
    private Button _confirmCancel;

    private static readonly Color DangerColor = new(0.95f, 0.45f, 0.4f);

    private ClassData _class;
    private CareerSettings _settings;
    private CareerCharacter _character;
    private readonly List<(CareerTalent Talent, Button Button)> _talentButtons = new();

    /// <summary>Each talent's "+" button; Autoplay presses them (--talents).</summary>
    public IReadOnlyList<(CareerTalent Talent, Button Button)> TalentButtons => _talentButtons;
    public Button ContinueButton => _continue;
    public Button ResetCharacterButton => _resetCharacter;
    public Button ConfirmResetButton => _confirmYes;
    public Button CancelResetButton => _confirmCancel;
    public bool ConfirmingReset => _confirm.Visible;
    public int FreePoints => _settings.PointsFree(_character);
    public CareerCharacter Character => _character;

    public override void _Ready()
    {
        var screen = UiStyles.ScreenLayout(this, 14);
        screen.CustomMinimumSize = new Vector2(640, 0);

        var layout = _content = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 14);
        screen.AddChild(layout);

        _title = UiStyles.Title("");
        layout.AddChild(_title);

        _xpBar = UiStyles.XpBar();
        layout.AddChild(_xpBar);
        _xpLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _xpLabel.AddThemeFontSizeOverride("font_size", 15);
        layout.AddChild(_xpLabel);

        _record = UiStyles.DimLabel("", 15);
        _record.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(_record);

        _heirloom = UiStyles.WrappedLabel("", 16);
        _heirloom.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(_heirloom);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        layout.AddChild(panel);
        var talents = new VBoxContainer();
        talents.AddThemeConstantOverride("separation", 10);
        panel.AddChild(talents);

        _points = UiStyles.WrappedLabel("", 20);
        talents.AddChild(_points);
        talents.AddChild(UiStyles.DimLabel("Talents apply to every career run of this class. Reset is free.", 13));
        _talentGrid = new GridContainer { Columns = 4 };
        _talentGrid.AddThemeConstantOverride("h_separation", 20);
        _talentGrid.AddThemeConstantOverride("v_separation", 6);
        talents.AddChild(_talentGrid);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 16);
        layout.AddChild(buttons);

        var back = new Button { Text = "Back", CustomMinimumSize = new Vector2(140, 44) };
        back.Pressed += () =>
        {
            Hide();
            EmitSignal(SignalName.BackRequested);
        };
        buttons.AddChild(back);

        _reset = new Button { Text = "Reset talents", CustomMinimumSize = new Vector2(160, 44) };
        _reset.Pressed += ResetTalents;
        buttons.AddChild(_reset);

        _continue = new Button { Text = "Choose spells", CustomMinimumSize = new Vector2(220, 44) };
        _continue.Pressed += () =>
        {
            Hide();
            EmitSignal(SignalName.ContinueRequested, _class);
        };
        buttons.AddChild(_continue);

        // Its own row, away from Choose spells: this one can't be undone.
        var danger = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        layout.AddChild(danger);
        _resetCharacter = new Button { Text = "Reset character", CustomMinimumSize = new Vector2(160, 36) };
        UiStyles.ColorButtonText(_resetCharacter, DangerColor);
        _resetCharacter.Pressed += AskResetCharacter;
        danger.AddChild(_resetCharacter);

        _confirm = new VBoxContainer { Visible = false };
        _confirm.AddThemeConstantOverride("separation", 20);
        screen.AddChild(_confirm);
        _confirmText = UiStyles.WrappedLabel("", 18);
        _confirmText.HorizontalAlignment = HorizontalAlignment.Center;
        _confirm.AddChild(_confirmText);
        var confirmButtons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        confirmButtons.AddThemeConstantOverride("separation", 16);
        _confirm.AddChild(confirmButtons);
        _confirmCancel = new Button { Text = "Cancel", CustomMinimumSize = new Vector2(180, 44) };
        _confirmCancel.Pressed += CancelResetCharacter;
        confirmButtons.AddChild(_confirmCancel);
        _confirmYes = new Button { Text = "Yes, reset character", CustomMinimumSize = new Vector2(220, 44) };
        UiStyles.ColorButtonText(_confirmYes, DangerColor);
        _confirmYes.Pressed += ResetCharacter;
        confirmButtons.AddChild(_confirmYes);
        Hide();
    }

    public void Open(ClassData playerClass, CareerSettings settings, CareerCharacter character, ItemInstance heirloom)
    {
        _class = playerClass;
        _settings = settings;
        _character = character;

        // Talent ranks bought under an older level curve can exceed what's earned now: refund them all.
        if (settings.PointsFree(character) < 0)
        {
            GD.Print($"[career] {Career.ClassId(playerClass)} has more talent ranks than points; resetting talents");
            character.Talents.Clear();
            Career.Instance.Persist();
        }

        _heirloom.Text = heirloom != null
            ? $"Heirloom: {heirloom.FullName} (starts equipped)  ·  {heirloom.Describe().Replace("\n", ", ")}"
            : "Heirloom: none yet. Win a run to keep one piece of gear for the next.";

        // Tinted by the heirloom's rarity, like everywhere else items are shown.
        _heirloom.AddThemeColorOverride("font_color", heirloom?.Color ?? UiStyles.Dim);

        BuildTalents();
        Refresh();
        _confirm.Hide();
        _content.Show();
        Show();
        _continue.GrabFocus();
    }

    public override void _Input(InputEvent @event)
    {
        if (Visible && _confirm.Visible && @event.IsActionPressed("pause"))
        {
            GetViewport().SetInputAsHandled();
            CancelResetCharacter();
        }
    }

    // Swaps the screen for the prompt, with Cancel focused so a stray Enter or Space erases nothing.
    private void AskResetCharacter()
    {
        _confirmText.Text = $"Are you sure you want to reset your {_class.DisplayName}?\n\n" +
                            $"This erases level {_settings.LevelFor(_character.Xp)}, {_character.Xp} XP, all talents, the heirloom and " +
                            $"the record ({_character.Runs} runs, {_character.Victories} victories) and starts over at level 1. " +
                            "Your other characters aren't affected. This can't be undone.";
        _content.Hide();
        _confirm.Show();
        _confirmCancel.GrabFocus();
    }

    private void CancelResetCharacter()
    {
        _confirm.Hide();
        _content.Show();
        _resetCharacter.GrabFocus();
    }

    private void ResetCharacter()
    {
        Career.Instance.ResetCharacter(_class);
        Open(_class, _settings, Career.Instance.For(_class), null);
    }

    private void BuildTalents()
    {
        foreach (var child in _talentGrid.GetChildren())
        {
            _talentGrid.RemoveChild(child);
            child.QueueFree();
        }
        _talentButtons.Clear();

        foreach (var talent in _settings.Talents)
        {
            // Plain labels: autowrapping ones collapse to zero width inside a GridContainer.
            _talentGrid.AddChild(GridLabel(talent.DisplayName, 17, Colors.White, 140));
            _talentGrid.AddChild(GridLabel($"{StatInfo.Format(talent.Stat, talent.PerRank)} per rank", 14, UiStyles.Dim, 200));
            _talentGrid.AddChild(GridLabel("", 15, UiStyles.Gold, 190));
            var button = new Button { Text = "+", CustomMinimumSize = new Vector2(44, 30) };
            button.Pressed += () => Buy(talent);
            _talentGrid.AddChild(button);
            _talentButtons.Add((talent, button));
        }
    }

    private static Label GridLabel(string text, int fontSize, Color color, float width)
    {
        var label = new Label { Text = text, CustomMinimumSize = new Vector2(width, 0), VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private void Buy(CareerTalent talent)
    {
        if (_settings.PointsFree(_character) <= 0 || _character.Rank(talent.Id) >= talent.MaxRank)
            return;
        _character.Talents[talent.Id] = _character.Rank(talent.Id) + 1;
        Career.Instance.Persist();
        Refresh();
    }

    private void ResetTalents()
    {
        _character.Talents.Clear();
        Career.Instance.Persist();
        Refresh();
    }

    private void Refresh()
    {
        int level = _settings.LevelFor(_character.Xp);
        _title.Text = $"{_class.DisplayName}  ·  Level {level}";

        if (level >= _settings.MaxLevel)
        {
            _xpBar.MaxValue = 1;
            _xpBar.Value = 1;
            _xpLabel.Text = $"{_character.Xp} XP  ·  max level";
        }
        else
        {
            int into = _character.Xp - _settings.XpForLevel(level);
            int needed = _settings.XpToNext(level);
            _xpBar.MaxValue = needed;
            _xpBar.Value = into;
            _xpLabel.Text = $"{into} / {needed} XP to level {level + 1}";
        }

        _record.Text = $"Runs {_character.Runs}  ·  Victories {_character.Victories}  ·  " +
                       $"Best wave {_character.BestWave}  ·  Kills {_character.Kills}";

        int free = _settings.PointsFree(_character);
        _points.Text = $"Talent points: {free} free of {_settings.PointsEarned(_character)}";
        _reset.Disabled = _settings.PointsSpent(_character) == 0;
        _resetCharacter.Disabled = _character.IsBlank();

        for (int i = 0; i < _talentButtons.Count; i++)
        {
            var (talent, button) = _talentButtons[i];
            int rank = System.Math.Min(_character.Rank(talent.Id), talent.MaxRank);
            var current = (Label)_talentGrid.GetChild(i * 4 + 2);
            current.Text = rank > 0 ? $"{talent.Describe(rank)} ({rank}/{talent.MaxRank})" : $"0/{talent.MaxRank}";
            button.Disabled = free <= 0 || rank >= talent.MaxRank;
        }
    }
}
