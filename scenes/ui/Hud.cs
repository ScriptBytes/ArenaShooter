using Godot;

namespace ArenaShooter;

/// <summary>
/// On-screen overlay. Runs while the game is paused (process_mode = Always in Hud.tscn)
/// so it can listen for the restart key on the end-of-round screen.
/// </summary>
public partial class Hud : CanvasLayer
{
    [Signal] public delegate void RestartRequestedEventHandler();

    private Label _time;
    private ProgressBar _healthBar;
    private Label _healthLabel;
    private Label _kills;
    private Label _message;

    public override void _Ready()
    {
        _time = GetNode<Label>("TimeLabel");
        _healthBar = GetNode<ProgressBar>("HealthBar");
        _healthLabel = GetNode<Label>("HealthBar/HealthLabel");
        _kills = GetNode<Label>("KillsLabel");
        _message = GetNode<Label>("MessageLabel");
    }

    public void SetTime(float secondsLeft) => _time.Text = Mathf.CeilToInt(secondsLeft).ToString();

    public void SetKills(int kills) => _kills.Text = $"Kills: {kills}";

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
