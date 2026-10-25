using Godot;

namespace ArenaShooter;

/// <summary>
/// On-screen overlay. Runs while the game is paused (process_mode = Always in Hud.tscn)
/// so it can listen for the restart key on the end-of-round screen. The arcade end screen is the
/// message plus two buttons: retry the same class and spells, or choose a class (R).
/// </summary>
public partial class Hud : CanvasLayer
{
    [Signal] public delegate void RestartRequestedEventHandler();
    [Signal] public delegate void RetryRequestedEventHandler();

    private Label _wave;
    private Label _time;
    private ProgressBar _healthBar;
    private Label _healthLabel;
    private Label _gold;
    private Label _bag;
    private Label _message;
    private HBoxContainer _abilityBar;
    private WeaponSlotView _weapon;
    private ItemInstance _weaponItem;
    private HBoxContainer _endButtons;
    private Button _retry;
    private ColorRect _endBackdrop;

    /// <summary>The end screen's buttons; Autoplay presses Retry (--retry).</summary>
    public Button RetryButton => _retry;
    public bool EndScreenVisible => _message.Visible;

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
        // Hotbar tooltips (weapon, spells, potions) styled like the shop's.
        _abilityBar.Theme = UiStyles.TooltipTheme();
        BuildEndButtons();
    }

    private void BuildEndButtons()
    {
        // Dims the frozen arena behind the end message so it stays readable.
        _endBackdrop = new ColorRect
        {
            Visible = false,
            Color = new Color(0.03f, 0.03f, 0.05f, 0.6f),
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_endBackdrop);
        MoveChild(_endBackdrop, _message.GetIndex());

        _endButtons = new HBoxContainer
        {
            Visible = false,
            Alignment = BoxContainer.AlignmentMode.Center,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -320f,
            OffsetRight = 320f,
            OffsetTop = 96f,
            OffsetBottom = 140f,
        };
        _endButtons.AddThemeConstantOverride("separation", 16);
        AddChild(_endButtons);

        _retry = new Button { CustomMinimumSize = new Vector2(260, 44) };
        _retry.Pressed += () => EmitSignal(SignalName.RetryRequested);
        _endButtons.AddChild(_retry);
        var choose = new Button { Text = "Choose class (R)", CustomMinimumSize = new Vector2(220, 44) };
        choose.Pressed += () => EmitSignal(SignalName.RestartRequested);
        _endButtons.AddChild(choose);
    }

    /// <summary>
    /// Builds the hotbar: the weapon, then slots 1–4 for the player's spells (empty slots show as blanks).
    /// </summary>
    public void BindAbilities(AbilityCaster caster)
    {
        foreach (var child in _abilityBar.GetChildren())
            child.QueueFree();

        _weapon = new WeaponSlotView { Caster = caster, Item = _weaponItem };
        _abilityBar.AddChild(_weapon);
        _abilityBar.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });

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

    /// <summary>The equipped main-hand item, shown in the weapon slot. Safe to call before <see cref="BindAbilities"/>.</summary>
    public void SetWeapon(ItemInstance item)
    {
        _weaponItem = item;
        if (_weapon != null)
            _weapon.Item = item;
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

    /// <summary>The arcade end screen: <paramref name="text"/> plus Retry (same class and spells) and Choose class.</summary>
    public void ShowEndScreen(string text, string retryLabel)
    {
        ShowMessage(text);
        _retry.Text = retryLabel;
        _endBackdrop.Show();
        _endButtons.Show();
        _retry.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_message.Visible && @event.IsActionPressed("restart"))
            EmitSignal(SignalName.RestartRequested);
    }
}
