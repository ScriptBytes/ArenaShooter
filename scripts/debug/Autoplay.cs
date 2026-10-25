using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Debug driver for testing with nobody at the keyboard (headless runs, cloud sessions).
/// Main only creates it when the game is launched with "-- --autoplay" (see AutoplayOptions).
/// It goes through the real UI by pressing the same buttons a player would, moves the player,
/// logs run events as "[autoplay] event key=value ...", and quits when the run ends.
/// </summary>
public partial class Autoplay : Node
{
    private const float CircleRadius = 260f;
    private const float CircleSpeed = 0.5f; // radians per second

    public AutoplayOptions Options { get; set; }
    public ModeSelect ModeSelect { get; set; }
    public ClassSelect ClassSelect { get; set; }
    public CareerScreen CareerScreen { get; set; }
    public CareerSummary CareerSummary { get; set; }
    public CareerSettings CareerSettings { get; set; }
    public SpellSelect SpellSelect { get; set; }
    public Shop Shop { get; set; }
    public Player Player { get; set; }
    public RunState Run { get; set; }
    public Vector2 ArenaCenter { get; set; }

    private const float DrinkCooldown = 0.5f;
    private const int BuffPotionMinEnemies = 5;

    private float _moveAngle;
    private float _drinkWait;
    private readonly HashSet<ConsumableData> _boughtThisShop = new();
    private bool _failed;
    private float _menuWait;
    private ClassData _class;
    private bool _careerScreenLogged;
    private bool _heirloomDone;
    private bool _runEnded;

    /// <summary>Runs finished in this app launch (static, so it survives the restart's scene reload).</summary>
    public static int RunsDone { get; private set; }

    public static void Log(string message) => GD.Print($"[autoplay] {message}");

    public override void _Ready()
    {
        // Menus are shown while the tree is paused, so this has to keep running then.
        ProcessMode = ProcessModeEnum.Always;

        if (Options.Errors.Count > 0)
            Fail(string.Join("; ", Options.Errors));
    }

    public override void _Process(double delta)
    {
        if (_failed)
            return;

        bool inMenu = ModeSelect.Visible || ClassSelect.Visible || CareerScreen.Visible || SpellSelect.Visible
                      || Shop.Visible || CareerSummary.Visible;
        if (inMenu && (_menuWait += (float)delta) < Options.MenuDelay)
            return;
        _menuWait = 0f;

        if (ModeSelect.Visible)
            PickMode();
        else if (ClassSelect.Visible)
            PickClass();
        else if (CareerScreen.Visible)
            CareerStep();
        else if (CareerSummary.Visible)
            SummaryStep();
        else if (SpellSelect.Visible)
            SpellStep();
        else if (Shop.Visible)
            ShopStep();
        else if (!GetTree().Paused)
        {
            Move((float)delta);
            MaybeDrink((float)delta);
        }
    }

    public void OnRunStarted(ClassData playerClass)
    {
        _class = playerClass;
        Combat.ResetDamageLog();
        string spells = string.Join(",", Player.Abilities.Spells.Select(s => s.Ability.DisplayName));
        Log($"run_start class={playerClass.DisplayName} spells=\"{spells}\" gear=\"{Gear()}\" wave={Options.StartWave} " +
            $"seed={(Options.Seed is { } seed ? (seed + (ulong)RunsDone).ToString() : "random")} dotnet={System.Environment.Version}");
        if (Options.AutoCastAll)
        {
            foreach (var slot in Player.Abilities.Spells)
                slot.AutoCast = true;
        }
    }

    public void OnWaveStarted(int wave)
    {
        _boughtThisShop.Clear();
        Log($"wave_start wave={wave} gold={Run.Gold} bag={Run.Bag} {PotionSummary()}");
    }

    public void OnWaveEnded(int wave) =>
        Log($"wave_end wave={wave} kills={Run.Kills} gold={Run.Gold} bag={Run.Bag} " +
            $"hp={Player.Health.Current:0}/{Player.Health.Max:0} {PotionSummary()} {Defense()}");

    // Run totals so far: hits that landed, hits dodged, HP healed by regen, damage absorbed by barriers.
    private string Defense() => $"hits={Player.HitsTaken} dodged={Player.HitsDodged} regen={Player.RegenHealed:0.#} shielded={Player.ShieldAbsorbed:0.#}";

    public void OnHeartCollected(float healed, float stored) =>
        Log($"heart healed={healed:0.#} added={stored:0.#} hp={Player.Health.Current:0}/{Player.Health.Max:0} reserve={Reserve()}");

    private string Reserve() => $"{Run.Potions.Reserve:0.#}/{Run.Potions.ReserveCapacity:0}";

    // Full health potions, the reserve, and each buff potion carried.
    private string PotionSummary()
    {
        var potions = Run.Potions;
        string buffs = string.Join(",", potions.Hotbar.Select(c => $"{c.DisplayName}:{potions.Count(c)}"));
        return $"potions={potions.HealthPotions} reserve={Reserve()} buffs=\"{buffs}\"";
    }

    public void OnRunEnded(bool victory)
    {
        Log($"run_end result={(victory ? "victory" : "defeat")} wave={Run.Wave} kills={Run.Kills} " +
            $"gold={Run.Gold} gear=\"{Gear()}\" trinkets={Run.Trinkets.Count} trinket_list=\"{string.Join(",", Run.Trinkets.Select(t => t.LogName))}\" {Defense()}");
        // Damage that landed per source (weapon, spell, minion type), biggest first.
        var damage = Combat.DamageBySource.OrderByDescending(p => p.Value).ToList();
        Log($"damage total={damage.Sum(p => p.Value):0} by=\"{string.Join(",", damage.Select(p => $"{p.Key}:{p.Value:0}"))}\"");
        _runEnded = true;
        if (CareerSummary.Visible)
            return; // SummaryStep banks the career, then quits or restarts.

        if (++RunsDone >= Options.Runs)
            GetTree().Quit();
        else
            Input.ParseInputEvent(new InputEventAction { Action = "restart", Pressed = true });
    }

    private void PickMode()
    {
        var choice = ModeSelect.Choices.FirstOrDefault(c => c.Mode == Options.Mode);
        Press(choice.Button);
    }

    private string CareerState(CareerCharacter character)
    {
        string talents = string.Join(",", CareerSettings.Talents
            .Where(t => character.Rank(t.Id) > 0).Select(t => $"{t.Id}:{character.Rank(t.Id)}"));
        return $"level={CareerSettings.LevelFor(character.Xp)} xp={character.Xp} points={CareerSettings.PointsFree(character)} " +
               $"talents=\"{talents}\" heirloom=\"{character.Heirloom}\" heirloom_rarity={character.HeirloomRarity ?? "-"} runs={character.Runs} victories={character.Victories}";
    }

    // Logs the character once, spends one point per frame (--talents=spread), then continues to spells.
    private void CareerStep()
    {
        var character = CareerScreen.Character;
        if (!_careerScreenLogged)
        {
            _careerScreenLogged = true;
            Log($"career save=\"{Career.Instance.SavePath}\" load={Career.Instance.LoadStatus.ToString().ToLower()} {CareerState(character)}");
        }

        if (Options.Talents == TalentStrategy.Spread && CareerScreen.FreePoints > 0)
        {
            var pick = CareerScreen.TalentButtons.Where(t => !t.Button.Disabled)
                .OrderBy(t => character.Rank(t.Talent.Id)).FirstOrDefault();
            if (pick.Button != null)
            {
                Press(pick.Button);
                Log($"talent id={pick.Talent.Id} rank={character.Rank(pick.Talent.Id)} points_left={CareerScreen.FreePoints}");
                return;
            }
        }

        Press(CareerScreen.ContinueButton);
    }

    // After a career run: keep an heirloom on victory (--heirloom), log the banked career, then quit or restart.
    private void SummaryStep()
    {
        if (!_runEnded)
            return;

        var character = Career.Instance.For(_class);
        if (!_heirloomDone)
        {
            _heirloomDone = true;
            var choice = CareerSummary.HeirloomChoices.Where(c => !c.Button.Disabled).OrderByDescending(c => c.Item.Price).FirstOrDefault();
            if (Options.Heirloom == HeirloomStrategy.Priciest && choice.Button != null)
            {
                Press(choice.Button);
                Log($"heirloom item=\"{choice.Item.DisplayName}\" rarity={choice.Item.Rarity.ToString().ToLower()}");
                return;
            }
        }

        Log($"career_end class={_class.DisplayName} {CareerState(character)}");
        if (++RunsDone >= Options.Runs)
            GetTree().Quit();
        else
            Press(CareerSummary.ContinueButton);
    }

    private string Gear() => string.Join(",", Run.Equipment.Worn.Select(i => i.LogName));

    /// <summary>Logs what the shop offers (with rarities), each time it opens or rerolls.</summary>
    public void OnOffersRolled() =>
        Log($"offers wave={Run.Wave} items=\"{string.Join(",", Shop.Offers.Select(o => $"{o.Item.LogName}:{o.Price}"))}\"");

    /// <summary>Resolves --item names against the shop's pool (display name, Id or .tres file name).</summary>
    public ItemData FindItem(IEnumerable<ItemData> pool, string name)
    {
        var item = pool.FirstOrDefault(i =>
            Same(i.DisplayName, name) || Same(i.Id, name) || Same(i.ResourcePath.GetFile().GetBaseName(), name));
        if (item == null)
            Fail($"unknown item '{name}'");
        return item;
    }

    private void PickClass()
    {
        var choices = ClassSelect.Choices;
        var choice = Options.ClassName == null ? choices.FirstOrDefault()
            : int.TryParse(Options.ClassName, out int index) ? choices.ElementAtOrDefault(index)
            : choices.FirstOrDefault(c => Same(c.Class.DisplayName, Options.ClassName) || Same(c.Class.Id, Options.ClassName));

        if (choice.Button == null)
        {
            Fail($"unknown class '{Options.ClassName}'");
            return;
        }

        Press(choice.Button);
    }

    // One pick per frame, like the shop. --spells picks those in order; otherwise the list's first N.
    private void SpellStep()
    {
        var choices = SpellSelect.Choices.ToList();
        var wanted = Options.Spells.Count > 0
            ? Options.Spells.Select(name => choices.FirstOrDefault(c =>
                Same(c.Spell.DisplayName, name) || Same(c.Spell.ResourcePath.GetFile().GetBaseName(), name))).ToList()
            : choices.ToList();

        int missing = wanted.FindIndex(c => c.Button == null);
        if (missing >= 0)
        {
            Fail($"unknown spell '{Options.Spells[missing]}' (class has: " +
                 $"{string.Join(", ", choices.Select(c => c.Spell.DisplayName))})");
            return;
        }

        if (SpellSelect.StartButton.Disabled)
        {
            var next = wanted.FirstOrDefault(c => !SpellSelect.Picks.Contains(c.Spell));
            if (next.Button == null || next.Button.Disabled)
            {
                Fail($"--spells needs exactly the class's pick count; got {Options.Spells.Count}");
                return;
            }
            Press(next.Button);
            return;
        }

        if (Options.Spells.Count > 0 && Options.Spells.Count != SpellSelect.Picks.Count)
        {
            Fail($"--spells needs exactly {SpellSelect.Picks.Count} spells; got {Options.Spells.Count}");
            return;
        }
        Press(SpellSelect.StartButton);
    }

    // One action per frame, like a player clicking, so each press sees the refreshed shop.
    // --buy-potion picks come first (one of each per visit), then the --shop strategy.
    private void ShopStep()
    {
        foreach (var name in Options.BuyPotions)
        {
            var potion = Shop.PotionOffers.FirstOrDefault(o =>
                Same(o.Item.DisplayName, name) || Same(o.Item.Id, name) || Same(o.Item.ResourcePath.GetFile().GetBaseName(), name));
            if (potion == null)
            {
                Fail($"unknown potion '{name}' (shop has: {string.Join(", ", Shop.PotionOffers.Select(o => o.Item.DisplayName))})");
                return;
            }
            if (_boughtThisShop.Contains(potion.Item) || potion.BuyButton.Disabled)
                continue;

            _boughtThisShop.Add(potion.Item);
            Log($"buy item=\"{potion.Item.DisplayName}\" price={potion.Price} gold_left={Run.Gold - potion.Price} replaces=\"\"");
            Press(potion.BuyButton);
            return;
        }

        var affordable = Shop.Offers.Where(o => !o.BuyButton.Disabled);
        var offer = Options.Shop switch
        {
            ShopStrategy.Cheapest => affordable.OrderBy(o => o.Price).FirstOrDefault(),
            ShopStrategy.First => affordable.FirstOrDefault(),
            _ => null,
        };

        if (offer != null)
        {
            // The first button is the first target slot (Shop builds them in TargetsFor order).
            var replaced = offer.Item.IsGear
                ? Run.Equipment.Displaced(offer.Item.Item, Run.Equipment.TargetsFor(offer.Item.Item)[0])
                : new List<ItemInstance>();
            Log($"buy item=\"{offer.Item.DisplayName}\" rarity={offer.Item.Rarity.ToString().ToLower()} price={offer.Price} " +
                $"gold_left={Run.Gold - offer.Price} stats=\"{offer.Item.Describe().Replace("\n", ", ")}\" " +
                $"replaces=\"{string.Join(",", replaced.Select(i => i.LogName))}\" refund={replaced.Sum(i => i.SellValue)}");
            Press(offer.BuyButton);
            return;
        }

        Press(Shop.ContinueButton);
    }

    // Health: drinks when the next potion's whole heal fits, or when under 35% HP (Space picks a full
    // reserve first, then a full potion, then a partial reserve). Buff potions: drinks one when a crowd is around and it isn't active.
    // Sends the real input actions. The press is handled later in the frame, so wait a bit between them.
    private void MaybeDrink(float delta)
    {
        if ((_drinkWait -= delta) > 0f || Player.Health.IsDead)
            return;

        var health = Player.Health;
        var source = Player.NextPotion;
        if (source != Player.PotionSource.None)
        {
            float heal = source == Player.PotionSource.Reserve ? Run.Potions.Reserve : Player.PotionHeal;
            if (health.Max - health.Current >= heal || health.Current < health.Max * 0.35f)
            {
                _drinkWait = DrinkCooldown;
                Log($"potion source={source.ToString().ToLower()} hp_before={health.Current:0}/{health.Max:0} " +
                    $"heal={heal:0.#} reserve={Reserve()}");
                Input.ParseInputEvent(new InputEventAction { Action = "use_potion", Pressed = true });
                return;
            }
        }

        var hotbar = Run.Potions.Hotbar;
        int enemies = GetTree().GetNodesInGroup(Groups.Enemies).Count;
        for (int i = 0; i < hotbar.Count; i++)
        {
            var potion = hotbar[i];
            if (Run.Potions.Count(potion) == 0 || enemies < BuffPotionMinEnemies
                || Player.Buffs.All.Any(b => b.Source == potion))
                continue;

            _drinkWait = DrinkCooldown;
            Log($"drink item=\"{potion.DisplayName}\" enemies={enemies} left={Run.Potions.Count(potion) - 1}");
            Input.ParseInputEvent(new InputEventAction { Action = $"consumable_{i + 1}", Pressed = true });
            return;
        }
    }

    private void Move(float delta)
    {
        if (Options.Move != MoveMode.Circle)
            return;

        _moveAngle += CircleSpeed * delta;
        var target = ArenaCenter + Vector2.FromAngle(_moveAngle) * CircleRadius;
        var direction = Player.GlobalPosition.DistanceTo(target) > 8f
            ? Player.GlobalPosition.DirectionTo(target)
            : Vector2.Zero;

        // Drives the same input actions the keyboard does, so Player code is untouched.
        SetAxis("move_left", "move_right", direction.X);
        SetAxis("move_up", "move_down", direction.Y);
    }

    private static void SetAxis(string negative, string positive, float value)
    {
        if (value > 0f)
        {
            Input.ActionRelease(negative);
            Input.ActionPress(positive, value);
        }
        else if (value < 0f)
        {
            Input.ActionRelease(positive);
            Input.ActionPress(negative, -value);
        }
        else
        {
            Input.ActionRelease(negative);
            Input.ActionRelease(positive);
        }
    }

    // Emitting Pressed skips the Disabled check a real click gets, so check it here.
    private static void Press(BaseButton button)
    {
        if (!button.Disabled)
            button.EmitSignal(BaseButton.SignalName.Pressed);
    }

    private void Fail(string reason)
    {
        Log($"error {reason}");
        _failed = true;
        GetTree().Quit(1);
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
