using System;
using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

public partial class Player : CharacterBody2D, IDamageable
{
    [Signal] public delegate void GoldCollectedEventHandler(int amount);

    /// <summary>A heart healed <paramref name="healed"/> HP and poured <paramref name="stored"/> into the potion reserve.</summary>
    [Signal] public delegate void HeartCollectedEventHandler(float healed, float stored);

    /// <summary>Space, keys 5+: what drinking will do next.</summary>
    public enum PotionSource { None, Charge, Bought, Reserve }

    // Base value; ApplyStats layers run bonuses on top. Other base stats come from ClassData.
    [Export] public float PickupRange { get; set; } = 80f;
    [Export] public float Radius { get; set; } = 16f;
    [Export] public float InvulnerabilityTime { get; set; } = 0.5f;
    [Export] public float HurtShake { get; set; } = 0.4f;

    /// <summary>Most HP life steal can restore per second, whatever the source (Brotato caps it too).</summary>
    [Export] public float LifeStealCap { get; set; } = 5f;

    /// <summary>Base heal of a health potion; the Potion Heal stat adds to it. Also the reserve's size.</summary>
    [ExportGroup("Potion")]
    [Export] public float BasePotionHeal { get; set; } = 10f;
    [Export] public int PotionsPerWave { get; set; } = 1;

    public static readonly Color HealColor = new("5ee07a");

    public float PotionHeal { get; private set; } = 10f;

    /// <summary>Charges, bought potions and the reserve. Owned by RunState, set in <see cref="Setup"/>.</summary>
    public PotionBelt Potions { get; private set; } = new();

    public float Speed { get; private set; } = 300f;

    /// <summary>Flat reduction to each hit taken (minimum 1 damage).</summary>
    public float Armor { get; private set; }

    public Health Health { get; private set; }
    public ShakeCamera Camera { get; private set; }
    public AbilityCaster Abilities { get; private set; }

    /// <summary>Timed bonuses from spells, layered on the run's stats. Cleared each wave.</summary>
    public ActiveBuffs Buffs { get; } = new();

    private ClassData _class;
    private PlayerStats _runStats = new();
    private Area2D _hurtbox;
    private float _invulnerableLeft;
    private float _basePickupRange;

    private Vector2 _leapVelocity;
    private float _leapLeft;
    private float _leapDuration;
    private bool _leapAirborne;
    private Action _onLanded;

    // Life steal: a token bucket that refills at LifeStealCap per second, plus healing not yet
    // shown, so a stream of tiny heals becomes one "+N" every few tenths of a second.
    private const float LifeStealTextInterval = 0.4f;
    private float _lifeStealBudget;
    private float _lifeStealShown;
    private float _lifeStealTextLeft;

    public bool IsLeaping => _leapLeft > 0f;

    /// <summary>Debug: ignore all damage (Autoplay's --god).</summary>
    public bool Invulnerable { get; set; }

    public override void _Ready()
    {
        AddToGroup(Groups.Player);
        AddToGroup(Groups.EnemyTargets);
        Health = GetNode<Health>("Health");
        Camera = GetNode<ShakeCamera>("Camera2D");
        Abilities = GetNode<AbilityCaster>("Abilities");
        _hurtbox = GetNode<Area2D>("Hurtbox");
        _basePickupRange = PickupRange;
    }

    /// <summary><paramref name="spells"/> are the ones picked from the class's list, in slot order.</summary>
    public void Setup(ClassData playerClass, IEnumerable<AbilityData> spells, PotionBelt potions)
    {
        _class = playerClass;
        Potions = potions;
        Abilities.Setup(spells);
    }

    /// <summary>Sets the basic attack from the main-hand weapon and its rarity's damage multiplier. Call before ApplyStats.</summary>
    public void SetWeapon(AbilityData weapon, float damageMultiplier = 1f) => Abilities.SetWeapon(weapon, damageMultiplier);

    /// <summary>Sets the run's stat bonuses and applies them, plus any active buffs.</summary>
    public void ApplyStats(PlayerStats runStats)
    {
        _runStats = runStats;
        RefreshStats();
    }

    public void AddBuff(Resource source, IReadOnlyList<StatModifier> modifiers, float duration, Color color)
    {
        Buffs.Add(source, modifiers, duration, color);
        RefreshStats();
    }

    public void ClearBuffs()
    {
        Buffs.Clear();
        RefreshStats();
    }

    private void RefreshStats()
    {
        var stats = _runStats;
        if (Buffs.All.Count > 0)
        {
            stats = _runStats.Copy();
            Buffs.AddTo(stats);
        }

        Speed = _class.MoveSpeed * stats.Multiplier(StatType.MoveSpeed, floor: 0.3f);
        PickupRange = Mathf.Max(_basePickupRange + stats.Get(StatType.PickupRange), 0f);
        Armor = _class.Armor + stats.Get(StatType.Armor);
        Health.SetMax(_class.MaxHealth + stats.Get(StatType.MaxHealth), refill: false);
        PotionHeal = Mathf.Max(BasePotionHeal + stats.Get(StatType.PotionHeal), 1f);
        Potions.ReserveCapacity = PotionHeal;
        Abilities.ApplyStats(stats);
    }

    public void CollectGold(int amount) => EmitSignal(SignalName.GoldCollected, amount);

    public void Heal(float amount)
    {
        float healed = Health.Heal(amount);
        if (healed > 0f)
            FloatingText.Spawn(GetTree().CurrentScene, GlobalPosition + new Vector2(0, -Radius),
                $"+{healed:0.#}", HealColor, 16);
    }

    /// <summary>Heals from life steal, limited to <see cref="LifeStealCap"/> HP per second.</summary>
    public void StealLife(float amount)
    {
        float healed = Health.Heal(Mathf.Min(amount, _lifeStealBudget));
        _lifeStealBudget -= healed;
        _lifeStealShown += healed;
    }

    /// <summary>Spends HP on a spell (Blood Pact). Never kills: leaves at least 1 HP.</summary>
    public void PayHealth(float amount)
    {
        float paid = Mathf.Min(amount, Health.Current - 1f);
        if (paid <= 0f)
            return;

        Health.TakeDamage(paid);
        FloatingText.SpawnDamage(GetTree().CurrentScene, GlobalPosition + new Vector2(0, -Radius), paid, new Color("ff5a5a"));
    }

    private void UpdateLifeSteal(float dt)
    {
        _lifeStealBudget = Mathf.Min(_lifeStealBudget + LifeStealCap * dt, LifeStealCap);
        _lifeStealTextLeft -= dt;
        if (_lifeStealTextLeft > 0f || _lifeStealShown < 0.1f)
            return;

        _lifeStealTextLeft = LifeStealTextInterval;
        FloatingText.Spawn(GetTree().CurrentScene, GlobalPosition + new Vector2(0, -Radius),
            $"+{_lifeStealShown:0.#}", HealColor, 14);
        _lifeStealShown = 0f;
    }

    public void RefillPotions() => Potions.RefillCharges(PotionsPerWave);

    /// <summary>A heart is worth picking up: you're hurt, or the reserve has room.</summary>
    public bool WantsHeart => !Health.IsDead && (Health.Current < Health.Max || !Potions.ReserveFull);

    /// <summary>
    /// Called by a heart. Hurt: it heals you, and only what's left over beyond full HP goes into
    /// the reserve. At full HP it all goes into the reserve (what doesn't fit is lost).
    /// </summary>
    public void CollectHeart(float amount)
    {
        float healed = Health.Heal(amount);
        float stored = Potions.FillReserve(amount - healed);
        var at = GlobalPosition + new Vector2(0, -Radius);
        if (healed > 0f)
            FloatingText.Spawn(GetTree().CurrentScene, at, $"+{healed:0.#}", HealColor, 16);
        if (stored > 0f)
            FloatingText.Spawn(GetTree().CurrentScene, at + new Vector2(0, -14f), $"+{stored:0.#}", Heart.HeartColor, 14);
        EmitSignal(SignalName.HeartCollected, healed, stored);
    }

    /// <summary>Which health potion Space would drink now (None at full health or with nothing left).</summary>
    public PotionSource NextPotion =>
        Health.IsDead || Health.Current >= Health.Max ? PotionSource.None
        : Potions.ReserveFull ? PotionSource.Reserve
        : Potions.Charges > 0 ? PotionSource.Charge
        : Potions.HealthPotions > 0 ? PotionSource.Bought
        : Potions.Reserve > 0f ? PotionSource.Reserve
        : PotionSource.None;

    /// <summary>
    /// Drinks a health potion (Space): the reserve first when it's full (making room for hearts), then a
    /// full potion (free charge, then bought), then the reserve whatever its fill. Does nothing at full
    /// health, so a misclick doesn't waste it.
    /// </summary>
    public bool UsePotion()
    {
        switch (NextPotion)
        {
            case PotionSource.Charge:
            case PotionSource.Bought:
                Potions.TakeHealthPotion();
                Heal(PotionHeal);
                return true;
            case PotionSource.Reserve:
                Heal(Potions.DrainReserve());
                return true;
            default:
                return false;
        }
    }

    /// <summary>Drinks a bought buff potion (keys 5+). Drinking one that's active refreshes its timer.</summary>
    public bool UseConsumable(ConsumableData consumable)
    {
        if (Health.IsDead || !Potions.Take(consumable))
            return false;

        AddBuff(consumable, consumable.Modifiers, consumable.Duration, consumable.IconColor);
        FloatingText.Spawn(GetTree().CurrentScene, GlobalPosition + new Vector2(0, -Radius - 12f),
            consumable.DisplayName, consumable.IconColor, 16);
        return true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("use_potion"))
        {
            UsePotion();
            GetViewport().SetInputAsHandled();
            return;
        }

        for (int i = 0; i < Potions.Hotbar.Count; i++)
        {
            if (!@event.IsActionPressed($"consumable_{i + 1}"))
                continue;
            UseConsumable(Potions.Hotbar[i]);
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    /// <summary>
    /// Jumps to <paramref name="target"/> over <paramref name="duration"/> seconds, untouchable
    /// while moving, then calls <paramref name="onLanded"/>. Walls cut the leap short.
    /// <paramref name="airborne"/> only changes the look: a jump arc, or a dash along the ground.
    /// </summary>
    public void Leap(Vector2 target, float duration, Action onLanded, bool airborne = true)
    {
        _leapAirborne = airborne;
        _leapDuration = _leapLeft = duration;
        _leapVelocity = (target - GlobalPosition) / duration;
        _onLanded = onLanded;
        _invulnerableLeft = Mathf.Max(_invulnerableLeft, duration + 0.15f);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        _invulnerableLeft -= dt;
        if (Buffs.Tick(dt))
            RefreshStats();
        UpdateLifeSteal(dt);
        QueueRedraw();

        if (IsLeaping)
        {
            Velocity = _leapVelocity;
            MoveAndSlide();

            _leapLeft -= dt;
            if (!IsLeaping)
            {
                var onLanded = _onLanded;
                _onLanded = null;
                onLanded?.Invoke();
            }
            return;
        }

        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = input * Speed;
        MoveAndSlide();

        if (_invulnerableLeft <= 0f)
            ApplyContactDamage();
    }

    public void TakeDamage(float amount)
    {
        if (Invulnerable || _invulnerableLeft > 0f || Health.IsDead)
            return;

        float taken = Mathf.Max(amount - Armor, 1f);
        Health.TakeDamage(taken);
        _invulnerableLeft = InvulnerabilityTime;
        Camera.AddTrauma(HurtShake);
        FloatingText.SpawnDamage(GetTree().CurrentScene, GlobalPosition + new Vector2(0, -Radius), taken, new Color("ff5a5a"));
    }

    // Enemies don't physically block the player; touching one hurts instead.
    // Take one hit from the strongest enemy touching us, then i-frames prevent stacking.
    private void ApplyContactDamage()
    {
        float strongest = 0f;
        foreach (var body in _hurtbox.GetOverlappingBodies())
        {
            if (body is Enemy enemy)
                strongest = Mathf.Max(strongest, enemy.ContactDamage);
        }

        if (strongest > 0f)
            TakeDamage(strongest);
    }

    public override void _Draw()
    {
        var color = _class?.Color ?? new Color("4aa3ff");
        DrawBuffRings();

        if (IsLeaping && !_leapAirborne)
        {
            // Dash: fading afterimages trail behind the body.
            var back = -_leapVelocity.Normalized();
            for (int i = 3; i >= 1; i--)
                DrawCircle(back * Radius * 0.9f * i, Radius * (1f - 0.12f * i), new Color(color, 0.5f - 0.12f * i));
            DrawCircle(Vector2.Zero, Radius, color);
            return;
        }

        if (IsLeaping)
        {
            // Fake height: a shadow stays on the ground while the body swells mid-air.
            float t = 1f - _leapLeft / _leapDuration;
            float lift = Mathf.Sin(Mathf.Pi * t);
            DrawCircle(Vector2.Zero, Radius * 0.9f, new Color(0, 0, 0, 0.35f));
            DrawCircle(new Vector2(0, -12f * lift), Radius * (1f + 0.35f * lift), color);
            return;
        }

        bool blink = _invulnerableLeft > 0f && (int)(_invulnerableLeft * 20f) % 2 == 0;
        DrawCircle(Vector2.Zero, Radius, blink ? Colors.White : color);
    }

    // One ring per active buff; the arc shrinks as the buff runs out.
    private void DrawBuffRings()
    {
        for (int i = 0; i < Buffs.All.Count; i++)
        {
            var buff = Buffs.All[i];
            float r = Radius + 5f + 5f * i;
            float sweep = Mathf.Tau * Mathf.Clamp(buff.TimeLeft / buff.Duration, 0f, 1f);
            DrawArc(Vector2.Zero, r, -Mathf.Pi / 2f, -Mathf.Pi / 2f + sweep, 32, buff.Color, 3f);
        }
    }
}
