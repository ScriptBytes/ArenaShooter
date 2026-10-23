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
    private Label _bag;
    private Label _message;
    private HBoxContainer _abilityBar;

    public override void _Ready()
    {
        _wave = GetNode<Label>("WaveLabel");
        _time = GetNode<Label>("TimeLabel");
        _healthBar = GetNode<ProgressBar>("HealthBar");
        _healthLabel = GetNode<Label>("HealthBar/HealthLabel");
        _gold = GetNode<Label>("GoldLabel");
        _bag = GetNode<Label>("BagLabel");
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

    /// <summary>
    /// Adds the potion slots after the spells: health potion (Space) and its heart-filled reserve,
    /// then one per buff potion (keys 5, 6…), shown even when none are owned so the keys stay put.
    /// </summary>
    public void BindPotions(Player player)
    {
        _abilityBar.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });
        _abilityBar.AddChild(new PotionSlotView { Player = player, Kind = PotionSlotView.SlotKind.Health });
        _abilityBar.AddChild(new PotionSlotView { Player = player, Kind = PotionSlotView.SlotKind.Reserve });

        if (player.Potions.Hotbar.Count == 0)
            return;
        _abilityBar.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });
        for (int i = 0; i < player.Potions.Hotbar.Count; i++)
        {
            _abilityBar.AddChild(new PotionSlotView
            {
                Player = player,
                Kind = PotionSlotView.SlotKind.Buff,
                Consumable = player.Potions.Hotbar[i],
                Hotkey = AbilityCaster.SpellSlotCount + i + 1,
            });
        }
    }

    public void SetTime(float secondsLeft) => _time.Text = Mathf.CeilToInt(secondsLeft).ToString();

    public void SetWave(int wave, int totalWaves) => _wave.Text = $"Wave {wave} / {totalWaves}";

    public void SetGold(int gold) => _gold.Text = $"Gold: {gold}";

    /// <summary>Shows the bag of leftover gold; hidden when it's empty.</summary>
    public void SetBag(int bag)
    {
        _bag.Text = $"Bag: {bag} (pickups x2)";
        _bag.Visible = bag > 0;
    }

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
