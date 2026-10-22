using Godot;

namespace ArenaShooter;

/// <summary>
/// Round controller: wires the scene together, runs the round timer, and decides win/loss.
/// </summary>
public partial class Main : Node2D
{
    private enum RoundState { Playing, Won, Lost }

    private const int CameraPadding = 80;

    [Export] public float RoundDuration { get; set; } = 60f;
    [Export] public Arena Arena { get; set; }
    [Export] public Player Player { get; set; }
    [Export] public EnemySpawner Spawner { get; set; }
    [Export] public Hud Hud { get; set; }

    private RoundState _state = RoundState.Playing;
    private float _timeLeft;
    private int _kills;

    public override void _Ready()
    {
        Player.Position = Arena.Size / 2f;
        Player.Camera.LimitLeft = -CameraPadding;
        Player.Camera.LimitTop = -CameraPadding;
        Player.Camera.LimitRight = (int)Arena.Size.X + CameraPadding;
        Player.Camera.LimitBottom = (int)Arena.Size.Y + CameraPadding;
        Player.Camera.ResetSmoothing();

        Player.Health.Changed += Hud.SetHealth;
        Player.Health.Died += () => EndRound(RoundState.Lost);
        Spawner.EnemyKilled += () => Hud.SetKills(++_kills);
        Hud.RestartRequested += Restart;

        _timeLeft = RoundDuration;
        Hud.SetTime(_timeLeft);
        Hud.SetHealth(Player.Health.Current, Player.Health.Max);
        Hud.SetKills(0);
        Spawner.Active = true;
    }

    public override void _Process(double delta)
    {
        if (_state != RoundState.Playing)
            return;

        _timeLeft = Mathf.Max(_timeLeft - (float)delta, 0f);
        Spawner.Progress = 1f - _timeLeft / RoundDuration;
        Hud.SetTime(_timeLeft);

        if (_timeLeft <= 0f)
            EndRound(RoundState.Won);
    }

    private void EndRound(RoundState result)
    {
        if (_state != RoundState.Playing)
            return;
        _state = result;

        if (result == RoundState.Won)
        {
            Spawner.Clear();
            Hud.ShowMessage($"Round complete!  Kills: {_kills}\nPress R to play again");
        }
        else
        {
            Spawner.Active = false;
            Hud.ShowMessage("Defeated\nPress R to try again");
        }

        GetTree().Paused = true;
    }

    private void Restart()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
