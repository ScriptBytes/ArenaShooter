using Godot;

namespace ArenaShooter;

/// <summary>
/// Run controller: wires the scene together and drives class select → wave → shop → wave loop.
/// The tree is paused whenever a wave isn't running; the UI layers run while paused.
/// </summary>
public partial class Main : Node2D
{
    [Signal] public delegate void RunStartedEventHandler(ClassData playerClass);
    [Signal] public delegate void WaveStartedEventHandler(int wave);
    [Signal] public delegate void WaveEndedEventHandler(int wave);
    [Signal] public delegate void RunEndedEventHandler(bool victory);

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
    private AutoplayOptions _autoplay;

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

        _autoplay = AutoplayOptions.FromCommandLine();
        if (_autoplay != null)
            SetUpAutoplay();

        _state = GameState.ClassSelect;
        GetTree().Paused = true;
        ClassSelect.Open(Classes);
    }

    private void StartRun(ClassData playerClass)
    {
        Player.Setup(playerClass);
        Player.ApplyStats(_run.Stats);
        Hud.BindAbilities(Player.Abilities);
        EmitSignal(SignalName.RunStarted, playerClass);
        StartWave(_autoplay?.StartWave ?? 1);
    }

    /// <summary>Debug mode for unattended runs (headless/cloud testing). See AutoplayOptions.</summary>
    private void SetUpAutoplay()
    {
        if (_autoplay.Seed is { } seed)
            GD.Seed(seed);

        var autoplay = new Autoplay
        {
            Options = _autoplay,
            ClassSelect = ClassSelect,
            Shop = Shop,
            Player = Player,
            Run = _run,
            ArenaCenter = Arena.Size / 2f,
        };
        AddChild(autoplay);

        RunStarted += autoplay.OnRunStarted;
        WaveStarted += autoplay.OnWaveStarted;
        WaveEnded += autoplay.OnWaveEnded;
        RunEnded += autoplay.OnRunEnded;

        // Run-start grants, applied before the class's first ApplyStats in StartRun.
        Player.Invulnerable = _autoplay.God;
        foreach (var (stat, value) in _autoplay.Stats)
            _run.Stats.Add(stat, value);
        foreach (var name in _autoplay.Items)
        {
            if (autoplay.FindItem(ItemCatalog, name) is { } item)
                _run.AddItem(item);
        }
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
        Hud.SetBag(_run.Bag);
        Hud.SetTime(_timeLeft);
        Spawner.Begin(_wave);

        GetTree().Paused = false;
        EmitSignal(SignalName.WaveStarted, wave);
    }

    private void EndWave()
    {
        BagFloorGold();
        Spawner.Clear();
        GetTree().CallGroup(Groups.Transient, Node.MethodName.QueueFree);
        EmitSignal(SignalName.WaveEnded, _run.Wave);

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
        EmitSignal(SignalName.RunEnded, result == GameState.Victory);
    }

    private void OnGoldCollected(int amount)
    {
        int bonus = _run.DrawFromBag(amount);
        _run.Gold += amount + bonus;
        Hud.SetGold(_run.Gold);

        if (bonus > 0)
        {
            Hud.SetBag(_run.Bag);
            FloatingText.Spawn(GetTree().CurrentScene, Player.GlobalPosition + new Vector2(0, -Player.Radius - 8f),
                "x2", Coin.GoldColor, 16);
        }
    }

    /// <summary>Moves the value of coins still on the floor into the bag, before they're cleared.</summary>
    private void BagFloorGold()
    {
        foreach (var node in GetTree().GetNodesInGroup(Groups.Transient))
        {
            if (node is Coin coin && !coin.IsQueuedForDeletion())
                _run.Bag += coin.Value;
        }
    }

    private void Restart()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
