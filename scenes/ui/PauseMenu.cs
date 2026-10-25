using System;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Esc menu during a wave or the shop: Resume, Settings, Retry (same class and spells), Abandon run
/// (back to class select in the same mode) and Quit game. The last three end the run, so they ask
/// first; in career mode the prompt says the run banks nothing. Pauses the game while open and
/// restores whatever pause state it found (the shop is paused already). Built in code.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void AbandonRequestedEventHandler();

    /// <summary>Set by Main: whether Esc may open the menu right now (a wave or the shop, nothing else on top).</summary>
    public Func<bool> CanOpen { get; set; } = () => false;

    /// <summary>Set by Main: whether this is a career run, for the prompts' wording.</summary>
    public Func<bool> IsCareer { get; set; } = () => false;

    public SettingsScreen Settings { get; set; }

    private VBoxContainer _menu;
    private VBoxContainer _confirm;
    private Label _confirmText;
    private Button _confirmYes;
    private Action _confirmed;
    private Button _resume;
    private Button _settings;
    private Button _retry;
    private bool _wasPaused;
    private Control _focusBefore;

    /// <summary>For Autoplay's --tour.</summary>
    public Button ResumeButton => _resume;
    public Button SettingsButton => _settings;

    public override void _Ready()
    {
        AddChild(new ColorRect { Color = new Color(0.03f, 0.03f, 0.05f, 0.8f), AnchorRight = 1f, AnchorBottom = 1f });
        var center = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(380, 0) };
        var style = UiStyles.Panel();
        style.SetContentMarginAll(24);
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);
        var stack = new VBoxContainer();
        panel.AddChild(stack);

        _menu = new VBoxContainer();
        _menu.AddThemeConstantOverride("separation", 12);
        stack.AddChild(_menu);
        _menu.AddChild(UiStyles.Title("Paused", 36));
        _resume = AddButton(_menu, "Resume (Esc)", Close);
        _settings = AddButton(_menu, "Settings", OpenSettings);
        _retry = AddButton(_menu, "Retry", () => Ask(
            "Start this run over with the same class and spells?", "Retry",
            () => EmitSignal(SignalName.RetryRequested)));
        AddButton(_menu, "Abandon run", () => Ask(
            "Abandon this run and go back to class select?", "Abandon",
            () => EmitSignal(SignalName.AbandonRequested)));
        AddButton(_menu, "Quit game", () => Ask("Quit the game?", "Quit", () => GetTree().Quit()));

        _confirm = new VBoxContainer { Visible = false };
        _confirm.AddThemeConstantOverride("separation", 16);
        stack.AddChild(_confirm);
        _confirmText = UiStyles.WrappedLabel("", 18);
        _confirmText.CustomMinimumSize = new Vector2(330, 0);
        _confirm.AddChild(_confirmText);
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 12);
        _confirm.AddChild(row);
        _confirmYes = AddButton(row, "", () => _confirmed?.Invoke());
        AddButton(row, "Cancel", ShowMenu);
        Hide();
    }

    /// <summary>Retry's label names the class: "Retry Mage (same spells)".</summary>
    public void SetClass(ClassData playerClass) => _retry.Text = $"Retry {playerClass.DisplayName} (same spells)";

    public void Open()
    {
        if (Visible)
            return;
        _wasPaused = GetTree().Paused;
        _focusBefore = GetViewport().GuiGetFocusOwner();
        GetTree().Paused = true;
        ShowMenu();
        Show();
    }

    public void Close()
    {
        if (!Visible)
            return;
        Hide();
        GetTree().Paused = _wasPaused;
        // Over the shop, give focus back to the button it was on (keyboard play).
        if (IsInstanceValid(_focusBefore) && _focusBefore.IsVisibleInTree())
            _focusBefore.GrabFocus();
        _focusBefore = null;
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("pause"))
            return;
        // Settings on top handles its own Esc.
        if (Settings?.Visible == true)
            return;

        if (Visible)
        {
            GetViewport().SetInputAsHandled();
            if (_confirm.Visible)
                ShowMenu();
            else
                Close();
        }
        else if (CanOpen())
        {
            GetViewport().SetInputAsHandled();
            Open();
        }
    }

    private void OpenSettings()
    {
        Settings.Open();
        Settings.Closed += BackFromSettings;
    }

    private void BackFromSettings()
    {
        Settings.Closed -= BackFromSettings;
        if (Visible)
            _settings.GrabFocus();
    }

    // Swaps the buttons for a yes/cancel prompt. A run ended this way banks nothing in career mode
    // (Goals.md: quitting mid-run banks nothing), so the prompt says so.
    private void Ask(string question, string yes, Action confirmed)
    {
        _confirmText.Text = IsCareer()
            ? $"{question}\n\nCareer: a run only banks XP when it ends in victory or defeat. Leaving now banks nothing."
            : $"{question}\n\nThis run's gold and items are lost.";
        _confirmYes.Text = yes;
        _confirmed = confirmed;
        _menu.Hide();
        _confirm.Show();
        _confirmYes.GrabFocus();
    }

    private void ShowMenu()
    {
        _confirm.Hide();
        _menu.Show();
        _resume.GrabFocus();
    }

    private static Button AddButton(Container parent, string text, Action pressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(200, 44) };
        button.Pressed += pressed;
        parent.AddChild(button);
        return button;
    }
}
