using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Run controller: wires the scene together and drives mode select → class select → (career screen) →
/// spell select → wave → shop → wave loop. The tree is paused whenever a wave isn't running (and while
/// the pause menu or gear screen is open); the UI layers run while paused. The mode, the career save
/// and a pending "retry same class" live in the <see cref="Career"/> autoload, which survives restarts.
/// </summary>
public partial class Main : Node2D
{
    [Signal] public delegate void RunStartedEventHandler(ClassData playerClass);
    [Signal] public delegate void WaveStartedEventHandler(int wave);
    [Signal] public delegate void WaveEndedEventHandler(int wave);
    [Signal] public delegate void RunEndedEventHandler(bool victory);

    private enum GameState { ClassSelect, Wave, Shop, Victory, Defeat }

    private const int CameraPadding = 80;

    [Export] public int TotalWaves { get; set; } = 20;
    [Export] public Godot.Collections.Array<ClassData> Classes { get; set; } = new();
    [Export] public ItemCatalog ItemCatalog { get; set; }
    [Export] public RaritySettings Rarities { get; set; }
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
    [Export] public PauseMenu PauseMenu { get; set; }
    [Export] public GearScreen GearScreen { get; set; }
    [Export] public SettingsScreen SettingsScreen { get; set; }

    private readonly RunState _run = new();
    private GameState _state;
    private WaveSettings _wave;
    private float _timeLeft;
    private AutoplayOptions _autoplay;
    private Autoplay _autoplayDriver;
    private ClassData _class;
    private Godot.Collections.Array<AbilityData> _spells;
    private Career Career => Career.Instance;
    private bool IsCareer => Career.Mode == GameMode.Career;

    /// <summary>What the shop can offer at random: the shared catalog items offered to this class plus its own weapons.</summary>
    private IEnumerable<ItemData> ShopPool => ItemCatalog.Items.Where(i => i.OfferedTo(_class)).Concat(_class.Weapons);

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
        Shop.Rarities = Rarities;
        Shop.Purchased += ApplyLoadout;
        Shop.GoldChanged += Hud.SetGold;
        Shop.ContinueRequested += () => StartWave(_run.Wave + 1);
        Hud.RestartRequested += Restart;
        Hud.RetryRequested += Retry;
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
        CareerSummary.RetryRequested += Retry;
        ModeSelect.SettingsRequested += SettingsScreen.Open;

        // Esc: a wave or the shop. Tab: a wave only (the shop shows your gear already). Never both at once.
        PauseMenu.Settings = SettingsScreen;
        PauseMenu.IsCareer = () => IsCareer;
        PauseMenu.CanOpen = () => _state is GameState.Wave or GameState.Shop && !GearScreen.Visible;
        PauseMenu.RetryRequested += Retry;
        PauseMenu.AbandonRequested += Restart;
        GearScreen.CanOpen = () => _state == GameState.Wave && !PauseMenu.Visible && !SettingsScreen.Visible;

        Career.ValidateIds(Classes,
            ItemCatalog.Items.Concat(ItemCatalog.Consumables)
                .Concat(Classes.SelectMany(c => c.Weapons.Append(c.StartingWeapon))),
            CareerSettings.Talents,
            Classes.SelectMany(c => c.Spells));
        var classIds = Classes.Select(Career.ClassId).ToHashSet();
        foreach (var item in ItemCatalog.Items)
            foreach (var id in (item.Classes ?? System.Array.Empty<string>()).Where(id => !classIds.Contains(id)))
                GD.PushError($"[items] {item.ResourcePath} is offered to unknown class Id '{id}'");

        _autoplay = AutoplayOptions.FromCommandLine();
        if (_autoplay != null)
            SetUpAutoplay();

        _state = GameState.ClassSelect;
        GetTree().Paused = true;
        // "Retry same class" skips every select screen. After a plain restart the mode is already
        // picked, so go straight to class select.
        if (TakeRetry() is var (retryClass, retrySpells))
        {
            if (_autoplayDriver != null)
                _autoplayDriver.FromRetry = true;
            StartRun(retryClass, retrySpells);
        }
        else if (Career.Mode == GameMode.None)
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

    /// <summary>The class's saved heirloom with its rarity, or null if none or its item no longer exists.</summary>
    private ItemInstance Heirloom(ClassData playerClass)
    {
        var character = Career.Peek(playerClass);
        string id = character.Heirloom;
        if (id == null)
            return null;
        var item = ItemCatalog.Items.Concat(playerClass.Weapons).FirstOrDefault(i => Career.ItemId(i) == id);
        if (item == null)
        {
            GD.PushWarning($"[career] heirloom '{id}' of {Career.ClassId(playerClass)} isn't a known item; ignored");
            return null;
        }

        // Saves from before rarity have no HeirloomRarity: Common.
        var rarity = Rarity.Common;
        if (character.HeirloomRarity != null && !System.Enum.TryParse(character.HeirloomRarity, ignoreCase: true, out rarity))
        {
            GD.PushWarning($"[career] heirloom rarity '{character.HeirloomRarity}' of {Career.ClassId(playerClass)} is unknown; using Common");
            rarity = Rarity.Common;
        }
        return new ItemInstance(item, Rarities.Tier(rarity));
    }

    private void StartRun(ClassData playerClass, Godot.Collections.Array<AbilityData> spells)
    {
        _class = playerClass;
        _spells = spells;
        // Starting weapons are always Common.
        _run.Equipment.StartingWeapon = new ItemInstance(playerClass.StartingWeapon, Rarities.Common);
        _run.AddItem(_run.Equipment.StartingWeapon);
        _run.Potions.SetCatalog(ItemCatalog.Consumables);
        if (IsCareer)
            ApplyCareer(playerClass);
        if (_autoplay != null)
            GrantAutoplayItems();

        Player.Setup(playerClass, spells, _run.Potions);
        ApplyLoadout();
        Hud.BindAbilities(Player.Abilities);
        Hud.BindPotions(Player);
        PauseMenu.SetClass(playerClass);
        GearScreen.Bind(_run, Player);
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
        if (_autoplay.Waves is { } waves)
            TotalWaves = waves;
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
            Hud = Hud,
            PauseMenu = PauseMenu,
            GearScreen = GearScreen,
            SettingsScreen = SettingsScreen,
            Player = Player,
            Run = _run,
            ArenaCenter = Arena.Size / 2f,
            ArenaBounds = Arena.Bounds,
            TotalWaves = TotalWaves,
            Spawner = Spawner,
        };
        AddChild(autoplay);

        RunStarted += autoplay.OnRunStarted;
        WaveStarted += autoplay.OnWaveStarted;
        WaveEnded += autoplay.OnWaveEnded;
        RunEnded += autoplay.OnRunEnded;
        Player.HeartCollected += autoplay.OnHeartCollected;
        Shop.OffersRolled += autoplay.OnOffersRolled;

        // Run-start grants, applied before the class's first ApplyStats in StartRun.
        Player.Invulnerable = _autoplay.God;
        Shop.ForcedRarity = _autoplay.Rarity;
        foreach (var (stat, value) in _autoplay.Stats)
            _run.GrantStat(stat, value);
    }

    // --item grants (at --rarity, default Common). They wait for the class, since its weapons are part of the pool.
    private void GrantAutoplayItems()
    {
        foreach (var name in _autoplay.Items)
        {
            // Any catalog item, even one restricted to another class: --item is a test hook.
            var item = _autoplayDriver.FindItem(ItemCatalog.Items.Concat(_class.Weapons).Concat(ItemCatalog.Consumables), name);
            if (item is ConsumableData potion)
                _run.AddPotion(potion);
            else if (item != null)
                _run.AddItem(new ItemInstance(item, Rarities.Tier(_autoplay.Rarity ?? Rarity.Common)));
        }
    }

    /// <summary>Pushes the run's gear and stats to the player. Call whenever either changes.</summary>
    private void ApplyLoadout()
    {
        Player.SetWeapon(_run.Equipment.Weapon, _run.Equipment.WeaponDamageMultiplier);
        Player.ApplyStats(_run.Stats);
        Hud.SetWeapon(_run.Equipment.MainHand ?? _run.Equipment.StartingWeapon);
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
        Minion.WaveHealthMultiplier = _wave.MinionHealthMultiplier;
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
            Hud.ShowEndScreen(result == GameState.Victory
                    ? $"Victory!\nSurvived all {TotalWaves} waves · {_run.Kills} kills"
                    : $"Defeated on wave {_run.Wave}\n{_run.Kills} kills",
                $"Retry {_class.DisplayName} (same spells)");

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
        var keepable = _run.Equipment.Worn.Where(i => i.Item != _class.StartingWeapon);
        CareerSummary.Open(_class, CareerSettings, character, reward, levelBefore, _run.Wave, Heirloom(_class), keepable);
    }

    private void OnHeirloomChosen(ItemInstance item)
    {
        var character = Career.For(_class);
        character.Heirloom = Career.ItemId(item.Item);
        character.HeirloomRarity = item.Rarity.ToString();
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

    /// <summary>Restarts with the same class and spell picks, skipping the select screens (and, in career, the talents screen).</summary>
    private void Retry()
    {
        Career.PendingRetry = new Career.RetryRequest(Career.ClassId(_class), _spells.Select(Career.SpellId).ToList());
        Restart();
    }

    /// <summary>Takes the pending retry, resolved against this scene's classes; null if none or it no longer resolves.</summary>
    private (ClassData Class, Godot.Collections.Array<AbilityData> Spells)? TakeRetry()
    {
        var retry = Career.PendingRetry;
        Career.PendingRetry = null;
        if (retry == null)
            return null;

        var playerClass = Classes.FirstOrDefault(c => Career.ClassId(c) == retry.ClassId);
        var spells = retry.SpellIds.Select(id => playerClass?.Spells.FirstOrDefault(s => Career.SpellId(s) == id)).ToList();
        if (playerClass == null || spells.Any(s => s == null))
        {
            GD.PushWarning($"[retry] class '{retry.ClassId}' or one of its spells no longer exists; back to class select");
            return null;
        }
        return (playerClass, new Godot.Collections.Array<AbilityData>(spells));
    }

    /// <summary>Reloads the scene: class select in the same mode (also how a run is abandoned, which banks nothing).</summary>
    private void Restart()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
