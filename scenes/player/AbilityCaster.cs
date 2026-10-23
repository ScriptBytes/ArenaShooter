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

    private readonly List<AbilitySlot> _spells = new();
    private AbilityContext _context;
    private float _weaponCooldownMultiplier = 1f;

    public override void _Ready()
    {
        _context = new AbilityContext(this);
    }

    public void Setup(IEnumerable<AbilityData> spells)
    {
        _spells.Clear();
        foreach (var spell in spells)
        {
            if (_spells.Count == SpellSlotCount)
                break;
            _spells.Add(new AbilitySlot(spell, spell.AutoCastByDefault));
        }
    }

    /// <summary>Swaps the basic attack (the main-hand weapon). Keeps the slot if it's unchanged.</summary>
    public void SetWeapon(AbilityData weapon)
    {
        if (Weapon?.Ability == weapon)
            return;
        Weapon = new AbilitySlot(weapon, autoCast: true) { CooldownMultiplier = _weaponCooldownMultiplier };
    }

    public void ApplyStats(PlayerStats stats)
    {
        _context.DamageMultiplier = stats.Multiplier(StatType.Damage);
        _context.RangeBonus = stats.Get(StatType.Range);
        _context.CritChanceBonus = stats.Get(StatType.CritChance);
        _weaponCooldownMultiplier = 1f / stats.Multiplier(StatType.AttackSpeed);
        if (Weapon != null)
            Weapon.CooldownMultiplier = _weaponCooldownMultiplier;
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

    private static void Tick(AbilitySlot slot, float dt)
    {
        slot.CooldownLeft = Mathf.Max(slot.CooldownLeft - dt, 0f);
    }

    private void TryAutoCast(AbilitySlot slot)
    {
        if (slot.IsReady && slot.Ability.TryAutoTarget(_context, out var target, out var targetEnemy))
            Cast(slot, target, targetEnemy);
    }

    private void Cast(AbilitySlot slot, Vector2 target, Enemy targetEnemy)
    {
        slot.Ability.Cast(_context, target, targetEnemy);
        slot.CooldownLeft = slot.EffectiveCooldown;
    }
}
