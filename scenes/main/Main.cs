using Godot;

namespace ArenaShooter;

/// <summary>
/// Run controller: wires the scene together and drives class select → wave → shop → wave loop.
/// The tree is paused whenever a wave isn't running; the UI layers run while paused.
/// </summary>
public partial class Main : Node2D
{
    private enum GameState { ClassSelect, Wave, Shop, Victory, Defeat }

    private const int CameraPadding = 80;

    [Export] public int TotalWaves { get; set; } = 5;
    [Export] public Godot.Collections.Array<ClassData> Classes { get; set; } = new();
    [Export] public ItemCatalog ItemCatalog { get; set; }
    [Export] public Arena Arena { get; set; }
    [Export] public Player Player { get; set; }
    [Export] public EnemySpawner Spawner { get; set; }
    [Export] public Hud Hud { get; set; }
    [Export] public Shop Shop { get; set; }
    [Export] public ClassSelect ClassSelect { get; set; }

    private readonly RunState _run = new();
    private GameState _state;
    private WaveSettings _wave;
    private float _timeLeft;

    public override void _Ready()
    {
        Player.Camera.LimitLeft = -CameraPadding;
        Player.Camera.LimitTop = -CameraPadding;
        Player.Camera.LimitRight = (int)Arena.Size.X + CameraPadding;
        Player.Camera.LimitBottom = (int)Arena.Size.Y + CameraPadding;

        Player.Health.Changed += Hud.SetHealth;
        Player.Health.Died += () => EndRun(GameState.Defeat);
        Player.GoldCollected += OnGoldCollected;
        Spawner.EnemyKilled += () => _run.Kills++;
        Shop.Purchased += () => Player.ApplyStats(_run.Stats);
        Shop.ContinueRequested += () => StartWave(_run.Wave + 1);
        Hud.RestartRequested += Restart;
        ClassSelect.ClassChosen += StartRun;

        _state = GameState.ClassSelect;
        GetTree().Paused = true;
        ClassSelect.Open(Classes);
    }

    private void StartRun(ClassData playerClass)
    {
        Player.Setup(playerClass);
        Player.ApplyStats(_run.Stats);
        Hud.BindAbilities(Player.Abilities);
        StartWave(1);
    }

    public override void _Process(double delta)
    {
        if (_state != GameState.Wave)
            return;

        _timeLeft = Mathf.Max(_timeLeft - (float)delta, 0f);
        Spawner.Progress = 1f - _timeLeft / _wave.Duration;
        Hud.SetTime(_timeLeft);

        if (_timeLeft <= 0f)
            EndWave();
    }

    private void StartWave(int wave)
    {
        _run.Wave = wave;
        _wave = WaveSettings.For(wave);
        _timeLeft = _wave.Duration;
        _state = GameState.Wave;

        Player.Position = Arena.Size / 2f;
        Player.Health.Refill();
        Player.Abilities.ResetCooldowns();

        Hud.SetWave(wave, TotalWaves);
        Hud.SetGold(_run.Gold);
        Hud.SetTime(_timeLeft);
        Spawner.Begin(_wave);

        GetTree().Paused = false;
    }

    private void EndWave()
    {
        Spawner.Clear();
        GetTree().CallGroup(Groups.Transient, Node.MethodName.QueueFree);

        if (_run.Wave >= TotalWaves)
        {
            EndRun(GameState.Victory);
            return;
        }

        _state = GameState.Shop;
        GetTree().Paused = true;
        Shop.Open(_run, ItemCatalog.Items);
    }

    private void EndRun(GameState result)
    {
        if (_state is GameState.Victory or GameState.Defeat)
            return;
        _state = result;
        Spawner.Active = false;

        Hud.ShowMessage(result == GameState.Victory
            ? $"Victory!\nSurvived all {TotalWaves} waves · {_run.Kills} kills\nPress R to play again"
            : $"Defeated on wave {_run.Wave}\n{_run.Kills} kills\nPress R to try again");

        GetTree().Paused = true;
    }

    private void OnGoldCollected(int amount)
    {
        _run.Gold += amount;
        Hud.SetGold(_run.Gold);
    }

    private void Restart()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
