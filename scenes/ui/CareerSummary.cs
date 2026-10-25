using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// End of a career run (instead of the HUD's end message): XP earned and its breakdown, level-ups,
/// and after a victory a choice of one worn piece of gear to keep as the heirloom. Choose class (or R)
/// restarts into class select; Retry starts the same class and spells again. The XP is already saved when this opens. Built in code.
/// </summary>
public partial class CareerSummary : CanvasLayer
{
    /// <summary>
    /// A plain C# event rather than a [Signal]: signal arguments must be Godot types, and an
    /// ItemInstance (item + rarity) is plain C#.
    /// </summary>
    public event System.Action<ItemInstance> HeirloomChosen;
    [Signal] public delegate void ContinueRequestedEventHandler();
    [Signal] public delegate void RetryRequestedEventHandler();

    private Label _title;
    private Label _breakdown;
    private Label _level;
    private ProgressBar _xpBar;
    private VBoxContainer _heirloomBox;
    private Label _heirloomLabel;
    private HBoxContainer _heirloomButtons;
    private Button _continue;
    private Button _retry;

    private readonly List<(ItemInstance Item, Button Button)> _heirloomChoices = new();

    /// <summary>"Keep" buttons for the worn gear after a victory (empty otherwise); Autoplay presses one (--heirloom).</summary>
    public IReadOnlyList<(ItemInstance Item, Button Button)> HeirloomChoices => _heirloomChoices;
    public Button ContinueButton => _continue;
    public Button RetryButton => _retry;

    public override void _Ready()
    {
        var layout = UiStyles.ScreenLayout(this, 14);
        layout.CustomMinimumSize = new Vector2(640, 0);

        _title = UiStyles.Title("", 40);
        layout.AddChild(_title);
        _breakdown = UiStyles.DimLabel("", 16);
        _breakdown.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(_breakdown);
        _level = UiStyles.WrappedLabel("", 22);
        _level.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(_level);
        _xpBar = UiStyles.XpBar();
        layout.AddChild(_xpBar);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        layout.AddChild(panel);
        _heirloomBox = new VBoxContainer();
        _heirloomBox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(_heirloomBox);
        _heirloomLabel = UiStyles.WrappedLabel("", 17);
        _heirloomBox.AddChild(_heirloomLabel);
        _heirloomButtons = new HBoxContainer();
        _heirloomButtons.AddThemeConstantOverride("separation", 8);
        _heirloomBox.AddChild(_heirloomButtons);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 16);
        layout.AddChild(buttons);
        // Retry skips class, talents and spell select; Continue goes back to class select.
        _retry = new Button { CustomMinimumSize = new Vector2(260, 44) };
        _retry.Pressed += () => EmitSignal(SignalName.RetryRequested);
        buttons.AddChild(_retry);
        _continue = new Button { Text = "Choose class (R)", CustomMinimumSize = new Vector2(220, 44) };
        _continue.Pressed += () => EmitSignal(SignalName.ContinueRequested);
        buttons.AddChild(_continue);
        Hide();
    }

    public void Open(ClassData playerClass, CareerSettings settings, CareerCharacter character,
        CareerSettings.Reward reward, int levelBefore, int wave, ItemInstance heirloom, IEnumerable<ItemInstance> keepable)
    {
        _title.Text = reward.Victory ? "Victory!" : $"Defeated on wave {wave}";
        _retry.Text = $"Retry {playerClass.DisplayName} (same spells)";

        var parts = new List<string> { $"{reward.Kills} kills +{reward.KillXp}", $"{reward.WavesCleared} waves cleared +{reward.WaveXp}" };
        if (reward.Victory)
            parts.Add($"victory +{reward.VictoryXp}");
        _breakdown.Text = $"+{reward.Total} XP   ({string.Join("  ·  ", parts)})";

        int level = settings.LevelFor(character.Xp);
        int gained = level - levelBefore;
        _level.Text = gained > 0
            ? $"{playerClass.DisplayName} reached level {level}!  +{gained} talent point{(gained > 1 ? "s" : "")}"
            : $"{playerClass.DisplayName}  ·  Level {level}";
        _level.AddThemeColorOverride("font_color", gained > 0 ? UiStyles.Gold : Colors.White);
        if (level >= settings.MaxLevel)
        {
            _xpBar.MaxValue = 1;
            _xpBar.Value = 1;
        }
        else
        {
            _xpBar.MaxValue = settings.XpToNext(level);
            _xpBar.Value = character.Xp - settings.XpForLevel(level);
        }

        BuildHeirloomChoices(reward.Victory, reward.Victory ? keepable.ToList() : new List<ItemInstance>(), heirloom);
        Show();
        _continue.GrabFocus();
    }

    private void BuildHeirloomChoices(bool victory, List<ItemInstance> keepable, ItemInstance heirloom)
    {
        foreach (var child in _heirloomButtons.GetChildren())
        {
            _heirloomButtons.RemoveChild(child);
            child.QueueFree();
        }
        _heirloomChoices.Clear();

        string current = heirloom != null ? $"Current heirloom: {heirloom.FullName}." : "No heirloom yet.";
        if (keepable.Count == 0)
        {
            _heirloomLabel.Text = victory
                ? $"{current} You wore no gear to keep beyond your starting weapon."
                : $"{current} Win a run to keep one piece of gear for the next.";
            return;
        }

        _heirloomLabel.Text = $"{current} Keep one piece of gear? It starts equipped in your next runs.";
        foreach (var item in keepable)
        {
            var button = new Button
            {
                Text = $"Keep {item.FullName}",
                TooltipText = item.Tooltip(),
                CustomMinimumSize = new Vector2(0, 36),
                Disabled = heirloom != null && item.Item == heirloom.Item && item.Rarity == heirloom.Rarity,
            };
            UiStyles.ColorButtonText(button, item.Color);
            button.Pressed += () => Choose(item);
            _heirloomButtons.AddChild(button);
            _heirloomChoices.Add((item, button));
        }
    }

    private void Choose(ItemInstance item)
    {
        foreach (var (_, button) in _heirloomChoices)
            button.Disabled = true;
        _heirloomLabel.Text = $"Heirloom: {item.FullName}. It starts equipped in your next runs.";
        HeirloomChosen?.Invoke(item);
        _continue.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Visible && @event.IsActionPressed("restart"))
            EmitSignal(SignalName.ContinueRequested);
    }
}
