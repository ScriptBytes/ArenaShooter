using Godot;

namespace ArenaShooter;

/// <summary>Runtime state for one equipped ability: cooldown and auto-cast toggle.</summary>
public class AbilitySlot
{
    public AbilitySlot(AbilityData ability, bool autoCast)
    {
        Ability = ability;
        AutoCast = autoCast;
    }

    public AbilityData Ability { get; }
    public bool AutoCast { get; set; }
    public float CooldownLeft { get; set; }

    /// <summary>Scales the base cooldown, e.g. attack speed for weapons.</summary>
    public float CooldownMultiplier { get; set; } = 1f;

    public float EffectiveCooldown => Ability.Cooldown * CooldownMultiplier;
    public bool IsReady => CooldownLeft <= 0f;

    /// <summary>1 right after casting, falling to 0 when ready.</summary>
    public float CooldownFraction =>
        EffectiveCooldown <= 0f ? 0f : Mathf.Clamp(CooldownLeft / EffectiveCooldown, 0f, 1f);
}
