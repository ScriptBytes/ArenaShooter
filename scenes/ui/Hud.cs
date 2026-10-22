using Godot;

namespace ArenaShooter;

/// <summary>
/// On-screen overlay. Runs while the game is paused (process_mode = Always in Hud.tscn)
/// so it can listen for the restart key on the end-of-round screen.
/// </summary>
public partial class Hud : CanvasLayer
{
    [Signal] public delegate void RestartRequestedEventHandler();

    private Label _wave;
    private Label _time;
    private ProgressBar _healthBar;
    private Label _healthLabel;
    private Label _gold;
    private Label _message;
    private HBoxContainer _abilityBar;

    public override void _Ready()
    {
        _wave = GetNode<Label>("WaveLabel");
        _time = GetNode<Label>("TimeLabel");
        _healthBar = GetNode<ProgressBar>("HealthBar");
        _healthLabel = GetNode<Label>("HealthBar/HealthLabel");
        _gold = GetNode<Label>("GoldLabel");
        _message = GetNode<Label>("MessageLabel");
        _abilityBar = GetNode<HBoxContainer>("AbilityBar");
    }

    /// <summary>Builds the 1–4 hotbar for the player's spells. Empty slots show as blanks.</summary>
    public void BindAbilities(AbilityCaster caster)
    {
        foreach (var child in _abilityBar.GetChildren())
            child.QueueFree();

        for (int i = 0; i < AbilityCaster.SpellSlotCount; i++)
            _abilityBar.AddChild(new AbilitySlotView { Index = i, Caster = caster });
    }

    public void SetTime(float secondsLeft) => _time.Text = Mathf.CeilToInt(secondsLeft).ToString();

    public void SetWave(int wave, int totalWaves) => _wave.Text = $"Wave {wave} / {totalWaves}";

    public void SetGold(int gold) => _gold.Text = $"Gold: {gold}";

    public void SetHealth(float current, float max)
    {
        _healthBar.MaxValue = max;
        _healthBar.Value = current;
        _healthLabel.Text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void ShowMessage(string text)
    {
        _message.Text = text;
        _message.Show();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_message.Visible && @event.IsActionPressed("restart"))
            EmitSignal(SignalName.RestartRequested);
    }
}
