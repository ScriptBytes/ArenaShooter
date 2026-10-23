using System;
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
    public ClassSelect ClassSelect { get; set; }
    public SpellSelect SpellSelect { get; set; }
    public Shop Shop { get; set; }
    public Player Player { get; set; }
    public RunState Run { get; set; }
    public Vector2 ArenaCenter { get; set; }

    private float _moveAngle;
    private int _potionWave;
    private bool _failed;

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

        if (ClassSelect.Visible)
            PickClass();
        else if (SpellSelect.Visible)
            SpellStep();
        else if (Shop.Visible)
            ShopStep();
        else if (!GetTree().Paused)
        {
            Move((float)delta);
            MaybeDrinkPotion();
        }
    }

    public void OnRunStarted(ClassData playerClass)
    {
        string spells = string.Join(",", Player.Abilities.Spells.Select(s => s.Ability.DisplayName));
        Log($"run_start class={playerClass.DisplayName} spells=\"{spells}\" wave={Options.StartWave} " +
            $"seed={Options.Seed?.ToString() ?? "random"} dotnet={System.Environment.Version}");
        if (Options.AutoCastAll)
        {
            foreach (var slot in Player.Abilities.Spells)
                slot.AutoCast = true;
        }
    }

    public void OnWaveStarted(int wave) =>
        Log($"wave_start wave={wave} gold={Run.Gold} bag={Run.Bag}");

    public void OnWaveEnded(int wave) =>
        Log($"wave_end wave={wave} kills={Run.Kills} gold={Run.Gold} bag={Run.Bag} " +
            $"hp={Player.Health.Current:0}/{Player.Health.Max:0}");

    public void OnRunEnded(bool victory)
    {
        Log($"run_end result={(victory ? "victory" : "defeat")} wave={Run.Wave} kills={Run.Kills} " +
            $"gold={Run.Gold} items={Run.Items.Count}");
        GetTree().Quit();
    }

    /// <summary>Resolves --item names against the catalog (display name or .tres file name).</summary>
    public ItemData FindItem(ItemCatalog catalog, string name)
    {
        var item = catalog.Items.FirstOrDefault(i =>
            Same(i.DisplayName, name) || Same(i.ResourcePath.GetFile().GetBaseName(), name));
        if (item == null)
            Fail($"unknown item '{name}'");
        return item;
    }

    private void PickClass()
    {
        var choices = ClassSelect.Choices;
        var choice = Options.ClassName == null ? choices.FirstOrDefault()
            : int.TryParse(Options.ClassName, out int index) ? choices.ElementAtOrDefault(index)
            : choices.FirstOrDefault(c => Same(c.Class.DisplayName, Options.ClassName));

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
    private void ShopStep()
    {
        var affordable = Shop.Offers.Where(o => !o.BuyButton.Disabled);
        var offer = Options.Shop switch
        {
            ShopStrategy.Cheapest => affordable.OrderBy(o => o.Price).FirstOrDefault(),
            ShopStrategy.First => affordable.FirstOrDefault(),
            _ => null,
        };

        if (offer != null)
        {
            Log($"buy item=\"{offer.Item.DisplayName}\" price={offer.Price} gold_left={Run.Gold - offer.Price}");
            Press(offer.BuyButton);
            return;
        }

        Press(Shop.ContinueButton);
    }

    // Drinks when the potion's full heal fits, or when low. Sends the real input action.
    private void MaybeDrinkPotion()
    {
        var health = Player.Health;
        if (Player.PotionsLeft == 0 || health.IsDead || _potionWave == Run.Wave)
            return;
        if (health.Max - health.Current < Player.PotionHeal && health.Current >= health.Max * 0.35f)
            return;

        // One try per wave: the press is handled later in the frame, so don't send it twice.
        _potionWave = Run.Wave;
        Log($"potion hp_before={health.Current:0}/{health.Max:0}");
        Input.ParseInputEvent(new InputEventAction { Action = "use_potion", Pressed = true });
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
