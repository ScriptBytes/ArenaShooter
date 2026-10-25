using System;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Settings, opened from mode select and from the pause menu. Edits <see cref="GameSettings"/>,
/// which applies and saves each change at once, so there's no Apply button. Back (or Esc) closes it
/// and returns to whichever screen opened it. Built in code.
/// </summary>
public partial class SettingsScreen : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private CheckButton _fullscreen;
    private HSlider _shake;
    private Label _shakeValue;
    private CheckButton _damageNumbers;
    private HSlider _volume;
    private Label _volumeValue;
    private Button _back;

    // Set while the controls are filled from the settings, so that doesn't count as a change.
    private bool _syncing;

    /// <summary>The controls, for Autoplay's --tour.</summary>
    public CheckButton DamageNumbersToggle => _damageNumbers;
    public Button BackButton => _back;

    public override void _Ready()
    {
        var layout = UiStyles.ScreenLayout(this, 18);
        layout.CustomMinimumSize = new Vector2(520, 0);
        layout.AddChild(UiStyles.Title("Settings", 40));

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiStyles.Panel());
        layout.AddChild(panel);
        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 14);
        panel.AddChild(grid);

        _fullscreen = AddToggle(grid, "Fullscreen", "F11 or Alt+Enter", on => GameSettings.Instance.SetFullscreen(on));
        (_shake, _shakeValue) = AddSlider(grid, "Screen shake", v => GameSettings.Instance.SetScreenShake(v));
        _damageNumbers = AddToggle(grid, "Damage numbers", "On enemies", on => GameSettings.Instance.SetDamageNumbers(on));
        (_volume, _volumeValue) = AddSlider(grid, "Master volume", v => GameSettings.Instance.SetMasterVolume(v));

        var note = UiStyles.DimLabel("There's no sound yet: the volume is kept for when there is. Settings save automatically.");
        note.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddChild(note);

        _back = new Button
        {
            Text = "Back",
            CustomMinimumSize = new Vector2(200, 44),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        _back.Pressed += Close;
        layout.AddChild(_back);

        // F11 changes fullscreen from any screen; keep the toggle in step.
        GameSettings.Instance.Changed += Sync;
        Hide();
    }

    public override void _ExitTree()
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.Changed -= Sync;
    }

    public void Open()
    {
        Sync();
        Show();
        _back.GrabFocus();
    }

    private void Close()
    {
        Hide();
        EmitSignal(SignalName.Closed);
    }

    public override void _Input(InputEvent @event)
    {
        if (Visible && @event.IsActionPressed("pause"))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    private void Sync()
    {
        var settings = GameSettings.Instance;
        _syncing = true;
        _fullscreen.ButtonPressed = settings.Fullscreen;
        _shake.Value = Mathf.Round(settings.ScreenShake * 100f);
        _shakeValue.Text = $"{settings.ScreenShake * 100f:0}%";
        _damageNumbers.ButtonPressed = settings.DamageNumbers;
        _volume.Value = Mathf.Round(settings.MasterVolume * 100f);
        _volumeValue.Text = $"{settings.MasterVolume * 100f:0}%";
        _syncing = false;
    }

    // Grid cells give wrapped labels no width, so the row names are plain labels with a minimum width.
    private static Label RowLabel(string text)
    {
        var label = new Label { Text = text, CustomMinimumSize = new Vector2(170, 0) };
        label.AddThemeFontSizeOverride("font_size", 18);
        return label;
    }

    private CheckButton AddToggle(GridContainer grid, string label, string hint, Action<bool> changed)
    {
        grid.AddChild(RowLabel(label));
        var toggle = new CheckButton { SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        toggle.Toggled += on =>
        {
            if (!_syncing)
                changed(on);
        };
        grid.AddChild(toggle);
        var note = new Label { Text = hint, CustomMinimumSize = new Vector2(130, 0) };
        note.AddThemeFontSizeOverride("font_size", 14);
        note.AddThemeColorOverride("font_color", UiStyles.Dim);
        grid.AddChild(note);
        return toggle;
    }

    private (HSlider, Label) AddSlider(GridContainer grid, string label, Action<float> changed)
    {
        grid.AddChild(RowLabel(label));
        var slider = new HSlider
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 5,
            CustomMinimumSize = new Vector2(220, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        var value = new Label { CustomMinimumSize = new Vector2(60, 0) };
        value.AddThemeFontSizeOverride("font_size", 16);
        slider.ValueChanged += v =>
        {
            value.Text = $"{v:0}%";
            if (!_syncing)
                changed((float)v / 100f);
        };
        grid.AddChild(slider);
        grid.AddChild(value);
        return (slider, value);
    }
}
