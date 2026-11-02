using System.Collections.Generic;
using Godot;

namespace ArenaShooter;

/// <summary>
/// Runs the player's weapon (always auto-attacks) and spell slots 1–4.
/// Keys 1–4 cast a spell manually, aimed at the mouse; Shift+key toggles its auto-cast.
/// </summary>
public partial class AbilityCaster : Node2D
{
    public const int SpellSlotCount = 4;

    public AbilitySlot Weapon { get; private set; }
    public IReadOnlyList<AbilitySlot> Spells => _spells;

    /// <summary>The caster's current stat modifiers. Minions read it to scale their hits.</summary>
    public AbilityContext Context => _context;

    private readonly List<AbilitySlot> _spells = new();
    private AbilityContext _context;
    // The weapon's own copy of the context, so its rarity can scale its damage without touching spells.
    private AbilityContext _weaponContext;
    private ClassData _class;
    private float _weaponCooldownMultiplier = 1f;
    private float _spellCooldownMultiplier = 1f;
    private float _weaponDamageMultiplier = 1f;

    public override void _Ready()
    {
        _context = new AbilityContext(this);
        _weaponContext = new AbilityContext(this);
    }

    public void Setup(IEnumerable<AbilityData> spells, ClassData playerClass)
    {
        _class = playerClass;
        _weaponContext.RaiseMinion = playerClass.WeaponRaiseMinion;
        _weaponContext.RaiseChance = playerClass.WeaponRaiseChance;
        _spells.Clear();
        foreach (var spell in spells)
        {
            if (_spells.Count == SpellSlotCount)
                break;
            _spells.Add(new AbilitySlot(spell, spell.AutoCastByDefault) { CooldownMultiplier = _spellCooldownMultiplier });
        }
    }

    /// <summary>
    /// Swaps the basic attack (the main-hand weapon). Keeps the slot if it's unchanged.
    /// <paramref name="damageMultiplier"/> is the weapon's rarity bonus; it applies from the next ApplyStats.
    /// </summary>
    public void SetWeapon(AbilityData weapon, float damageMultiplier = 1f)
    {
        _weaponDamageMultiplier = damageMultiplier;
        if (Weapon?.Ability == weapon)
            return;
        Weapon = new AbilitySlot(weapon, autoCast: true) { CooldownMultiplier = _weaponCooldownMultiplier };
    }

    public void ApplyStats(PlayerStats stats)
    {
        Fill(_context, stats, 1f);
        Fill(_weaponContext, stats, _weaponDamageMultiplier);
        _weaponContext.LifeStealBonus += _class?.WeaponLifeSteal ?? 0f;
        _weaponCooldownMultiplier = 1f / stats.Multiplier(StatType.AttackSpeed);
        if (Weapon != null)
            Weapon.CooldownMultiplier = _weaponCooldownMultiplier;

        // Cooldown Reduction: spells only (the weapon has Attack Speed), capped. A cooldown already
        // running keeps its remaining time; the next cast uses the new multiplier.
        _spellCooldownMultiplier = 1f - StatInfo.Capped(StatType.CooldownReduction, stats.Get(StatType.CooldownReduction)) / 100f;
        foreach (var slot in _spells)
            slot.CooldownMultiplier = _spellCooldownMultiplier;
    }

    private static void Fill(AbilityContext context, PlayerStats stats, float damageMultiplier)
    {
        context.DamageMultiplier = stats.Multiplier(StatType.Damage) * damageMultiplier;
        context.RangeBonus = stats.Get(StatType.Range);
        context.CritChanceBonus = stats.Get(StatType.CritChance);
        context.LifeStealBonus = stats.Get(StatType.LifeSteal);
        context.MinionDamageMultiplier = Mathf.Max(
            1f + (stats.Get(StatType.Damage) + stats.Get(StatType.MinionDamage)) / 100f, 0.1f);
    }

    public void ResetCooldowns()
    {
        if (Weapon != null)
            Weapon.CooldownLeft = 0f;
        foreach (var slot in _spells)
            slot.CooldownLeft = 0f;
    }

    public void ToggleAutoCast(int index)
    {
        if (index < 0 || index >= _spells.Count)
            return;

        _spells[index].AutoCast = !_spells[index].AutoCast;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Weapon == null)
            return;

        float dt = (float)delta;
        Tick(Weapon, dt);
        TryAutoCast(Weapon);

        foreach (var slot in _spells)
        {
            Tick(slot, dt);
            if (slot.AutoCast)
                TryAutoCast(slot);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        for (int i = 0; i < SpellSlotCount; i++)
        {
            if (!@event.IsActionPressed($"ability_{i + 1}"))
                continue;

            if (Input.IsKeyPressed(Key.Shift))
                ToggleAutoCast(i);
            else
                CastManually(i);

            GetViewport().SetInputAsHandled();
            return;
        }
    }

    private void CastManually(int index)
    {
        if (index >= _spells.Count || !_spells[index].IsReady)
            return;

        var slot = _spells[index];
        var target = slot.Ability.NeedsAim ? GetGlobalMousePosition() : GlobalPosition;
        Cast(slot, target, null);
    }

    private AbilityContext ContextFor(AbilitySlot slot) => slot == Weapon ? _weaponContext : _context;

    private static void Tick(AbilitySlot slot, float dt)
    {
        slot.CooldownLeft = Mathf.Max(slot.CooldownLeft - dt, 0f);
    }

    private void TryAutoCast(AbilitySlot slot)
    {
        if (slot.IsReady && slot.Ability.TryAutoTarget(ContextFor(slot), out var target, out var targetEnemy))
            Cast(slot, target, targetEnemy);
    }

    private void Cast(AbilitySlot slot, Vector2 target, Enemy targetEnemy)
    {
        slot.Ability.Cast(ContextFor(slot), target, targetEnemy);
        slot.CooldownLeft = slot.EffectiveCooldown;
    }
}
