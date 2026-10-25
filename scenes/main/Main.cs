using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Run controller: wires the scene together and drives mode select → class select → (career screen) →
/// spell select → wave → shop → wave loop. The tree is paused whenever a wave isn't running; the UI
/// layers run while paused. The mode and the career save live in the <see cref="Career"/> autoload.
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
    [Export] public SpellSelect SpellSelect { get; set; }
    [Export] public ModeSelect ModeSelect { get; set; }
    [Export] public CareerScreen CareerScreen { get; set; }
    [Export] public CareerSummary CareerSummary { get; set; }
    [Export] public CareerSettings CareerSettings { get; set; }

    private readonly RunState _run = new();
    private GameState _state;
    private WaveSettings _wave;
    private float _timeLeft;
    private AutoplayOptions _autoplay;
    private Autoplay _autoplayDriver;
    private ClassData _class;
    private Career Career => Career.Instance;
    private bool IsCareer => Career.Mode == GameMode.Career;

    /// <summary>What the shop can offer at random: the shared catalog plus the class's own weapons.</summary>
    private IEnumerable<ItemData> ShopPool => ItemCatalog.Items.Concat(_class.Weapons);

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
        Shop.Purchased += ApplyLoadout;
        Shop.ContinueRequested += () => StartWave(_run.Wave + 1);
        Hud.RestartRequested += Restart;
        ModeSelect.ModeChosen += mode =>
        {
            Career.Mode = (GameMode)mode;
            OpenClassSelect();
        };
        ClassSelect.BackRequested += ModeSelect.Open;
        ClassSelect.ClassChosen += OnClassChosen;
        CareerScreen.BackRequested += OpenClassSelect;
        CareerScreen.ContinueRequested += SpellSelect.Open;
        SpellSelect.BackRequested += () =>
        {
            if (IsCareer)
                OnClassChosen(_class);
            else
                OpenClassSelect();
        };
        SpellSelect.SpellsChosen += StartRun;
        CareerSummary.HeirloomChosen += OnHeirloomChosen;
        CareerSummary.ContinueRequested += Restart;

        Career.ValidateIds(Classes,
            ItemCatalog.Items.Concat(ItemCatalog.Consumables)
                .Concat(Classes.SelectMany(c => c.Weapons.Append(c.StartingWeapon))),
            CareerSettings.Talents);

        _autoplay = AutoplayOptions.FromCommandLine();
        if (_autoplay != null)
            SetUpAutoplay();

        _state = GameState.ClassSelect;
        GetTree().Paused = true;
        // After a restart the mode is already picked, so go straight to class select.
        if (Career.Mode == GameMode.None)
            ModeSelect.Open();
        else
            OpenClassSelect();
    }

    private void OpenClassSelect()
    {
        if (IsCareer)
            ClassSelect.Open(Classes, c =>
            {
                var character = Career.Peek(c);
                int free = CareerSettings.PointsFree(character);
                return $"Level {CareerSettings.LevelFor(character.Xp)}" + (free > 0 ? $"  ·  {free} talent point{(free > 1 ? "s" : "")} to spend" : "");
            });
        else
            ClassSelect.Open(Classes);
    }

    private void OnClassChosen(ClassData playerClass)
    {
        _class = playerClass;
        if (IsCareer)
            CareerScreen.Open(playerClass, CareerSettings, Career.For(playerClass), Heirloom(playerClass));
        else
            SpellSelect.Open(playerClass);
    }

    /// <summary>The class's saved heirloom, or null if none or its item no longer exists.</summary>
    private ItemData Heirloom(ClassData playerClass)
    {
        string id = Career.Peek(playerClass).Heirloom;
        if (id == null)
            return null;
        var item = ItemCatalog.Items.Concat(playerClass.Weapons).FirstOrDefault(i => Career.ItemId(i) == id);
        if (item == null)
            GD.PushWarning($"[career] heirloom '{id}' of {Career.ClassId(playerClass)} isn't a known item; ignored");
        return item;
    }

    private void StartRun(ClassData playerClass, Godot.Collections.Array<AbilityData> spells)
    {
        _class = playerClass;
        _run.Equipment.StartingWeapon = playerClass.StartingWeapon;
        _run.AddItem(playerClass.StartingWeapon);
        _run.Potions.SetCatalog(ItemCatalog.Consumables);
        if (IsCareer)
            ApplyCareer(playerClass);
        if (_autoplay != null)
            GrantAutoplayItems();

        Player.Setup(playerClass, spells, _run.Potions);
        ApplyLoadout();
        Hud.BindAbilities(Player.Abilities);
        Hud.BindPotions(Player);
        EmitSignal(SignalName.RunStarted, playerClass);
        StartWave(_autoplay?.StartWave ?? 1);
    }

    /// <summary>Career bonuses for a run: talent stats as grants, the heirloom equipped from the start.</summary>
    private void ApplyCareer(ClassData playerClass)
    {
        foreach (var (stat, value) in CareerSettings.Bonuses(Career.Peek(playerClass)))
            _run.GrantStat(stat, value);
        if (Heirloom(playerClass) is { } heirloom)
            _run.AddItem(heirloom);
    }

    /// <summary>Debug mode for unattended runs (headless/cloud testing). See AutoplayOptions.</summary>
    private void SetUpAutoplay()
    {
        // A scratch save by default, so tests never touch the player's career. Only the first launch's call counts.
        Career.Configure(_autoplay.SavePath, _autoplay.ResetCareer);
        if (_autoplay.Seed is { } seed)
            GD.Seed(seed + (ulong)Autoplay.RunsDone);

        var autoplay = _autoplayDriver = new Autoplay
        {
            Options = _autoplay,
            ModeSelect = ModeSelect,
            ClassSelect = ClassSelect,
            CareerScreen = CareerScreen,
            CareerSummary = CareerSummary,
            CareerSettings = CareerSettings,
            SpellSelect = SpellSelect,
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
        Player.HeartCollected += autoplay.OnHeartCollected;

        // Run-start grants, applied before the class's first ApplyStats in StartRun.
        Player.Invulnerable = _autoplay.God;
        foreach (var (stat, value) in _autoplay.Stats)
            _run.GrantStat(stat, value);
    }

    // --item grants. They wait for the class, since its weapons are part of the pool.
    private void GrantAutoplayItems()
    {
        foreach (var name in _autoplay.Items)
        {
            if (_autoplayDriver.FindItem(ShopPool.Concat(ItemCatalog.Consumables), name) is { } item)
                _run.AddItem(item);
        }
    }

    /// <summary>Pushes the run's gear and stats to the player. Call whenever either changes.</summary>
    private void ApplyLoadout()
    {
        Player.SetWeapon(_run.Equipment.Weapon);
        Player.ApplyStats(_run.Stats);
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
        Player.ClearBuffs();
        Player.RefillPotions();
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
        Shop.Open(_run, ShopPool);
    }

    private void EndRun(GameState result)
    {
        if (_state is GameState.Victory or GameState.Defeat)
            return;
        _state = result;
        Spawner.Active = false;

        GetTree().Paused = true;
        if (IsCareer)
            EndCareerRun(result == GameState.Victory);
        else
            Hud.ShowMessage(result == GameState.Victory
                ? $"Victory!\nSurvived all {TotalWaves} waves · {_run.Kills} kills\nPress R to play again"
                : $"Defeated on wave {_run.Wave}\n{_run.Kills} kills\nPress R to try again");

        EmitSignal(SignalName.RunEnded, result == GameState.Victory);
    }

    /// <summary>Banks the run's XP and record into the career (saved now), then shows the summary.</summary>
    private void EndCareerRun(bool victory)
    {
        var character = Career.For(_class);
        int levelBefore = CareerSettings.LevelFor(character.Xp);
        var reward = CareerSettings.RewardFor(_run.Kills, victory ? _run.Wave : _run.Wave - 1, victory);

        character.Xp += reward.Total;
        character.Runs++;
        character.Kills += _run.Kills;
        character.BestWave = Mathf.Max(character.BestWave, _run.Wave);
        if (victory)
            character.Victories++;
        Career.Persist();

        // Any worn gear except the class's starting weapon, which you always have anyway.
        var keepable = _run.Equipment.Worn.Where(i => i != _class.StartingWeapon);
        CareerSummary.Open(_class, CareerSettings, character, reward, levelBefore, _run.Wave, Heirloom(_class), keepable);
    }

    private void OnHeirloomChosen(ItemData item)
    {
        Career.For(_class).Heirloom = Career.ItemId(item);
        Career.Persist();
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
